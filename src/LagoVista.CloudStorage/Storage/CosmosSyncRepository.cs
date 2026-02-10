using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LagoVista.Core.Attributes.EntityDescriptionAttribute;
using static LagoVista.Core.Models.AdaptiveCard.MSTeams;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// A small, independent repository focused on sync operations:
    ///  1) summary list by entityType (+ optional search)
    ///  2) full JSON by id (query-based)
    ///  3) raw JSON upsert (stream-based) + optional optimistic concurrency by _etag
    /// </summary>
    public class CosmosSyncRepository : ISyncRepository
    {
        private readonly CosmosClient _client;
        private readonly Container _container;
        private readonly ISyncConnectionSettings _options;
        private readonly ILogger _logger;
        private readonly IFkIndexTableWriterBatched _fkWriter;
        private readonly INodeLocatorTableWriterBatched _nodeLocatorWriter;
        private readonly INodeLocatorTableReader _nodeLocator;
        private readonly ICacheProvider _cacheProvider;
        private readonly string _dbName;

        public const int DEFAULT_TAKE = 200;
        public const string FIXED_PARITIONKEY = null;

        public CosmosSyncRepository(ISyncConnectionSettings options, IFkIndexTableWriterBatched fkWriter, INodeLocatorTableWriterBatched nodeLocatorWriter, INodeLocatorTableReader nodeLocator, ICacheProvider cacheProvider, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fkWriter = fkWriter ?? throw new ArgumentNullException(nameof(fkWriter));
            _nodeLocatorWriter = nodeLocatorWriter ?? throw new ArgumentNullException(nameof(nodeLocatorWriter));
            _nodeLocator = nodeLocator ?? throw new ArgumentNullException(nameof(nodeLocator));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _dbName = _options.SyncConnectionSettings.ResourceName;
            _client = new CosmosClient(_options.SyncConnectionSettings.Uri, _options.SyncConnectionSettings.AccessKey, new CosmosClientOptions
            {
            });

            _container = _client.GetContainer(_options.SyncConnectionSettings.ResourceName, $"{_options.SyncConnectionSettings.ResourceName}_Collections");
        }

        public static string NormalizeAlphaNumericKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var sb = new StringBuilder(input.Length);

            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }

            if (sb.Length == 0) return null;

            // Ensure first character is a letter
            if (!char.IsLetter(sb[0]))
            {
                sb.Insert(0, 'a');
            }

            return sb.ToString();
        }

        public const string NOT_FOUND_ID = "09AE184AE5374B40B0E174D8F4956653";
        public const string NOT_FOUND_OWNER_ORG_ID = "00000000000000000000000000000000";
        public const string NOT_FOUND_KEY = "recordnotfound";
        public const string NOT_FOUND_TEXT = "Record Not Found";
        public const string NOT_FOUND_ENTITYTYPE = "RecordNotFound";

        private Dictionary<string, EntityHeader> _inMemoryCache = new Dictionary<string, EntityHeader>();

        public async Task<EntityHeader> GetEntityHeaderForRecordAsync(string id, CancellationToken ct = default)
        {
            lock (_inMemoryCache)
            {
                if (_inMemoryCache.ContainsKey(id))
                {
                    return _inMemoryCache[id];
                }
            }

            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));
            _logger.Trace($"{this.Tag()} - Request object for id {id}");
            var sql = @"SELECT c.id, c.Key, c.Name, c.Namespace, c.UserName, c.EntityType, c.OwnerOrganization, c.IsPublic
FROM c
where c.id = @id";

            var qd = new QueryDefinition(sql).WithParameter("@id", id);

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = Math.Min(1, 1)
            };

            if (!string.IsNullOrWhiteSpace(FIXED_PARITIONKEY))
            {
                requestOptions.PartitionKey = new PartitionKey(FIXED_PARITIONKEY);
            }

            using var iterator = _container.GetItemQueryIterator<EntityHeaderRow>(qd, requestOptions: requestOptions);

            if (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                var record = page.Resource?.FirstOrDefault();
                if (record != null)
                {
                    var eh = new EntityHeader()
                    {
                        Id = record.Id,
                        Key = record.GetKey(),
                        Text = record.Name,
                        OwnerOrgId = record.OwnerOrganization?.Id,
                        IsPublic = record.IsPublic,
                        EntityType = record.EntityType
                    };
                    lock (_inMemoryCache)
                    {
                        if (!_inMemoryCache.ContainsKey(eh.Id))
                            _inMemoryCache.Add(eh.Id, eh);
                    }
                    return eh;
                }
            }

            return new EntityHeader()
            {
                Id = NOT_FOUND_ID,
                Key = NOT_FOUND_KEY,
                Text = NOT_FOUND_TEXT,
                OwnerOrgId = NOT_FOUND_OWNER_ORG_ID,
                EntityType = NOT_FOUND_ENTITYTYPE
            };
        }

        public async Task<IReadOnlyList<SyncEntitySummary>> GetSummariesAsync(string entityType, string ownerOrganizationId, string search = null, int take = 200, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("entityType is required.", nameof(entityType));
            if (take <= 0) take = DEFAULT_TAKE;

            _logger.Trace($"{this.Tag()} - Request object of entity type {entityType}");

            // Projection-only query: cheap RU and fast.
            // We keep it tolerant: if some fields are missing, Cosmos returns null/defaults.
            var sql =
                "SELECT " +
                "  c.id, c.EntityType, c.Key, c.Name, " +
                "  c.Revision, c.RevisionTimeStamp, c._etag, " +
                "  c.IsDeleted, c.IsDeprecated, c.IsDraft, c.LastUpdatedDate, c.Sha256Hex " +
                "FROM c " +
                "WHERE c.EntityType = @entityType " +
                "  AND (NOT IS_DEFINED(c.IsDeleted) or c.IsDeleted = false)" +
                "  AND (IS_NULL(@search) OR @search = ''" +
                "       OR CONTAINS(LOWER(c.Name), @search) " +
                "       OR CONTAINS(LOWER(c.Key), @search)) " +
                "  AND c.OwnerOrganization.Id = @ownerOrganizationId " +
                "ORDER BY c.Name";


            _logger.Trace($"{this.Tag()} - Query {sql}");

            var qd = new QueryDefinition(sql)
                .WithParameter("@entityType", entityType)
                .WithParameter("@ownerOrganizationId", ownerOrganizationId)
                .WithParameter("@search", string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant());

            var results = new List<SyncEntitySummary>(Math.Min(take, 512));

            // Fixed PK makes cross-store easier if you're truly single-partition or known partition value.
            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = Math.Min(take, 200)
            };

            if (!string.IsNullOrWhiteSpace(FIXED_PARITIONKEY))
            {
                requestOptions.PartitionKey = new PartitionKey(FIXED_PARITIONKEY);
            }

            using var iterator = _container.GetItemQueryIterator<SyncEntitySummary>(
                qd,
                requestOptions: requestOptions);

            while (iterator.HasMoreResults && results.Count < take)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                results.AddRange(page.Resource);

                if (results.Count >= take) break;
            }

            _logger.Trace($"{this.Tag()} - Retrieved {results.Count} summaries for entity type {entityType}");

            // Defensive normalization: entityType/key should be there, but trim for stable UI.
            foreach (var r in results)
            {
                r.EntityType = r.EntityType?.Trim();
                r.Key = r.Key?.Trim();
                r.Name = r.Name?.Trim();
            }

            return results;
        }

        public async Task<string> GetJsonByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));

            // Query-by-id avoids needing partitionKey. Small datasets -> acceptable.
            const string sql = "SELECT * FROM c WHERE c.id = @id";
            var qd = new QueryDefinition(sql)
                .WithParameter("@id", id.Trim());

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 1
            };

            if (!string.IsNullOrWhiteSpace(FIXED_PARITIONKEY))
            {
                requestOptions.PartitionKey = new PartitionKey(FIXED_PARITIONKEY);
            }

            using var iterator = _container.GetItemQueryIterator<JObject>(
                qd,
                requestOptions: requestOptions);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                var doc = page.Resource?.FirstOrDefault();
                if (doc == null) continue;

                // Return raw JSON for UI side-by-side display.
                return doc.ToString(Formatting.Indented);
            }

            return null;
        }

        public async Task<string> GetOwnedJsonByIdAsync(string id, string ownerOrganizationId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));

            // Query-by-id avoids needing partitionKey. Small datasets -> acceptable.
            const string sql = "SELECT * FROM c WHERE c.id = @id and c.OwnerOrganization.Id = @ownerOrganizationId";
            var qd = new QueryDefinition(sql)
                .WithParameter("@id", id.Trim())
                .WithParameter("@ownerOrganizationId", ownerOrganizationId);

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 1
            };

            if (!string.IsNullOrWhiteSpace(FIXED_PARITIONKEY))
            {
                requestOptions.PartitionKey = new PartitionKey(FIXED_PARITIONKEY);
            }


            using var iterator = _container.GetItemQueryIterator<JObject>(
                qd,
                requestOptions: requestOptions);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                var doc = page.Resource?.FirstOrDefault();
                if (doc == null) continue;

                // Return raw JSON for UI side-by-side display.
                return doc.ToString(Formatting.Indented);
            }

            return null;
        }

        public async Task<string> GetJsonByEntityTypeAndKeyAsync(string key, string entityType, string ownerOrganizationId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key is required.", nameof(key));

            // Query-by-id avoids needing partitionKey. Small datasets -> acceptable.
            const string sql = "SELECT * FROM c WHERE c.id = @id and c.EntityType = @entityType and c.OwnerOrganization.Id = @ownerOrganizationId";
            var qd = new QueryDefinition(sql)
                .WithParameter("@key", key.Trim())
                .WithParameter("@entityType", entityType.Trim())
                .WithParameter("@ownerOrganizationId", ownerOrganizationId);

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 1
            };


            if (!string.IsNullOrWhiteSpace(FIXED_PARITIONKEY))
            {
                requestOptions.PartitionKey = new PartitionKey(FIXED_PARITIONKEY);
            }


            using var iterator = _container.GetItemQueryIterator<JObject>(
                qd,
                requestOptions: requestOptions);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                var doc = page.Resource?.FirstOrDefault();
                if (doc == null) continue;

                // Return raw JSON for UI side-by-side display.
                return doc.ToString(Formatting.Indented);
            }

            return null;
        }

        private async Task<SyncUpsertResult> UpsertJsonAsync(JObject doc, string expectedETag = null, CancellationToken ct = default)
        {
            _logger.Trace($"{this.Tag()} - Apply");

            // Validate minimum shape.
            var id = doc["id"]?.Value<string>()?.Trim();
            var entityType = doc["EntityType"]?.Value<string>()?.Trim();
            var key = doc["Key"]?.Value<string>()?.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                Debugger.Break();
                return new SyncUpsertResult()
                {
                    StatusCode = 500,
                    Messsage = $"Entity missing id - should never happen."
                };
            }

            if (string.IsNullOrWhiteSpace(entityType))
            {
                Debugger.Break();
                return new SyncUpsertResult()
                {
                    StatusCode = 500,
                    Messsage = $"Entity with ID: {id} missing entity type."
                };
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                if (entityType == "VerificationResults" || entityType == "CalendarEvent" || entityType == "UserFavorites" || entityType == "MostRecentlyUsed" || entityType == "Meeting")
                    doc["key"] = Guid.NewGuid().ToId().ToLower();
                else
                {
                    Debugger.Break();
                    return new SyncUpsertResult()
                    {
                        StatusCode = 500,
                        Messsage = $"Entity with ID: {id} of type {entityType} missing key."
                    };
                }
            }

            _logger.Trace($"{this.Tag()} - Apply", id.ToKVP("id"), key.ToKVP("key"), entityType.ToKVP("entityType"));

            // Ensure trimmed values are persisted.
            doc[nameof(EntityBase.EntityType)] = entityType;
            doc[nameof(EntityBase.Key)] = key;
            doc[nameof(EntityBase.Sha256Hex)] = EntityHasher.CalculateHash(doc);
            // We use stream APIs to avoid binding to any model types.
            var bytes = System.Text.Encoding.UTF8.GetBytes(doc.ToString(Formatting.None));
            using var ms = new MemoryStream(bytes);

            var requestOptions = new ItemRequestOptions();
            if (!string.IsNullOrWhiteSpace(expectedETag))
            {
                requestOptions.IfMatchEtag = expectedETag;
            }

            // If you truly have a fixed/single partition key value, this makes writes deterministic.
            PartitionKey? pk = null;
            if (!string.IsNullOrWhiteSpace(FIXED_PARITIONKEY))
            {
                pk = new PartitionKey(FIXED_PARITIONKEY);
            }



            ResponseMessage resp;
            if (pk.HasValue)
            {
                resp = await _container.UpsertItemStreamAsync(
                    ms,
                    pk.Value,
                    requestOptions,
                    cancellationToken: ct).ConfigureAwait(false);
            }
            else
            {
                resp = await _container.UpsertItemStreamAsync(
                    ms,
                    partitionKey: PartitionKey.None,
                    requestOptions: requestOptions,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            // Surface common concurrency conflicts clearly.
            if (resp.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(), "Upsert failed due to ETag mismatch (412 Precondition Failed).");
                throw new InvalidOperationException("Upsert failed due to ETag mismatch (412 Precondition Failed).");
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = resp.Content != null ? await new StreamReader(resp.Content).ReadToEndAsync().ConfigureAwait(false) : null;

                _logger.AddCustomEvent(LogLevel.Error, this.Tag(), "No success code updating", body.ToKVP("error"));
                throw new InvalidOperationException($"Upsert failed ({(int)resp.StatusCode} {resp.StatusCode}). {body}");
            }

            string returnedEtag = resp.Headers?.ETag;
            if (string.IsNullOrWhiteSpace(returnedEtag))
            {
                // Cosmos also includes _etag in the response body, but that requires parsing the stream again.
                // Header ETag is usually present; keep it optional.
                returnedEtag = null;
            }

            _logger.Trace($"{this.Tag()} - Success", resp.StatusCode.ToString().ToKVP("responseCode"));

            return new SyncUpsertResult
            {
                Id = id,
                ETag = returnedEtag,
                StatusCode = (int)resp.StatusCode,
                RequestCharge = resp.Headers?.RequestCharge
            };
        }

        private string GetCacheKey(string entityType, string id)
        {
            return $"{_dbName}-{entityType}-{id}".ToLower();
        }

        public async Task<SyncUpsertResult> UpsertJsonAsync(string json, EntityHeader org, EntityHeader user, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("json is required.", nameof(json));
            _logger.Trace($"{this.Tag()} - Starting ");

            JObject doc;
            try
            {
                doc = JObject.Parse(json);
                var id = doc["id"].Value<string>();
                var key = doc[nameof(EntityBase.Key)].Value<string>();
                var entityType = doc[nameof(EntityBase.EntityType)].Value<string>();
                _logger.Trace($"{this.Tag()} - Parsed JSON", id.ToKVP("id"), key.ToKVP("key"), entityType.ToKVP("entityType"));

                var existing = await GetJsonByIdAsync(id, ct);
                if (String.IsNullOrEmpty(existing))
                {
                    existing = await GetJsonByEntityTypeAndKeyAsync(key, entityType, org.Id);
                }

                if (String.IsNullOrEmpty(existing))
                {
                    _logger.Trace($"{this.Tag()} - No matching record", id.ToKVP("id"), key.ToKVP("key"), entityType.ToKVP("entityType"));
                    doc[nameof(EntityBase.CreatedBy)] = JToken.FromObject(user);
                    doc[nameof(EntityBase.CreationDate)] = DateTime.UtcNow.ToJSONString();
                }
                else
                {
                    var entity = JsonConvert.DeserializeObject<EntityBase>(existing);
                    _logger.Trace($"{this.Tag()} - Found Exisitng Record", id.ToKVP("id"), key.ToKVP("key"), entityType.ToKVP("entityType"));
                    id = entity.Id;
                    doc["id"] = id;
                }

                doc[nameof(EntityBase.DatabaseName)] = _dbName;
                doc[nameof(EntityBase.OwnerOrganization)] = JToken.FromObject(org);
                doc[nameof(EntityBase.LastUpdatedBy)] = JToken.FromObject(user);
                doc[nameof(EntityBase.LastUpdatedDate)] = DateTime.UtcNow.ToJSONString();


                var result = await UpsertJsonAsync(doc, null, ct);

                await _cacheProvider.RemoveAsync(GetCacheKey(entityType, id));

                if (entityType == "Module")
                {
                    await _cacheProvider.RemoveAsync(ALL_MODULES_CACHE_KEY);
                    await _cacheProvider.RemoveAsync($"{MODULE_CACHE_KEY}{key}");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("json must be a valid JSON object.", nameof(json), ex);
            }
        }


        public Task<SyncUpsertResult> UpsertJsonAsync(string json, string expectedETag = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("json is required.", nameof(json));

            JObject doc;
            try
            {
                doc = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("json must be a valid JSON object.", nameof(json), ex);
            }

            return UpsertJsonAsync(doc, expectedETag, ct);
        }

        public sealed class CosmosScanRow
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("EntityType")]
            public string EntityType { get; set; }

            // Cosmos system property for concurrency/change detection
            [JsonProperty("_etag")]
            public string ETag { get; set; }
        }

        /// <summary>
        /// Scans a container using Cosmos paging and returns the continuation token to resume later.
        /// Persist the returned continuationToken somewhere durable.
        /// </summary>
        public async Task<string> ScanContainerAsync(Func<CosmosScanRow, CancellationToken, Task> handleRowAsync,
            string continuationToken = null, string entityType = null, int pageSize = 100, int maxPagesThisRun = 10, string fixedPartitionKey = null, CancellationToken ct = default)
        {
            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = pageSize
            };

            if (!string.IsNullOrWhiteSpace(fixedPartitionKey))
            {
                requestOptions.PartitionKey = new PartitionKey(fixedPartitionKey);
            }

            var query = "SELECT c.id, c.EntityType, c._etag FROM c";
            if (!String.IsNullOrEmpty(entityType))
                query += " WHERE c.EntityType = @entityType";

            // Minimal SELECT keeps RU and payload down. Add fields as needed.
            var qd = !String.IsNullOrEmpty(entityType) ?
                new QueryDefinition(query).WithParameter("@entityType", entityType) :
                new QueryDefinition(query);

            using var iterator = _container.GetItemQueryIterator<CosmosScanRow>(qd, continuationToken: continuationToken, requestOptions: requestOptions);
            var pagesRead = 0;

            while (iterator.HasMoreResults && pagesRead < maxPagesThisRun)
            {
                FeedResponse<CosmosScanRow> page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                pagesRead++;
                var dop = 16;
                using var gate = new SemaphoreSlim(dop);

                var tasks = page.Select(async row =>
                {
                    await gate.WaitAsync(ct);
                    try
                    {
                        await handleRowAsync(row, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        gate.Release();
                    }
                });

                await Task.WhenAll(tasks);

                // This is the real “resume from here” cursor.
                continuationToken = page.ContinuationToken;
            }

            return continuationToken; // null means you're done (or you started at end)
        }

        public async Task<InvokeResult> SetEntityHashAsync(string id, CancellationToken ct = default)
        {
            var json = await GetJsonByIdAsync(id, ct);
            if (String.IsNullOrEmpty(json))
                return InvokeResult.FromError($"Could not load entity for id {id}");

            var token = JToken.Parse(json);
            var entity = JsonConvert.DeserializeObject<EntityBase>(json);
            if (entity.EntityType == "AppUser")
            {
                var userName = token["UserName"]?.Value<string>();
                if (userName == null)
                {
                    userName = token["Email"]?.Value<string>();
                    token["UserName"] = userName;
                }
                token["Key"] = NormalizeAlphaNumericKey(userName);
            }

            if (entity.EntityType == "Organization")
            {
                token["Key"] = token["Namespace"]?.Value<string>();
            }

            var key = token["Key"]?.Value<string>();
            if (key == null)
            {
                token["Key"] = Guid.NewGuid().ToId().ToLowerInvariant();
            }

            token["Sha256Hex"] = EntityHasher.CalculateHash(token.DeepClone());
            var bytes = System.Text.Encoding.UTF8.GetBytes(token.ToString(Formatting.None));
            using var ms = new MemoryStream(bytes);

            var requestOptions = new ItemRequestOptions();
            var cacheKey = GetCacheKey(entity.EntityType, entity.Id);
            try
            {
                var resp = await _container.UpsertItemStreamAsync(ms, PartitionKey.None, requestOptions, ct).ConfigureAwait(false);
                await _cacheProvider.RemoveAsync(cacheKey);

                return resp.IsSuccessStatusCode ? InvokeResult.Success : InvokeResult.FromError($"Error upserting entity with id {id} to set hash. Response code: {resp.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Error upserting entity with id {id} to set hash.", ex.Message.ToKVP("exception"));
                return InvokeResult.FromException(this.Tag(), ex);
            }

        }

        public async Task<InvokeResult<EhResolvedEntity>> ResolveEntityHeadersAsync(string id, CancellationToken ct = default, bool dryRun = false)
        {
            var json = await GetJsonByIdAsync(id);
            var entity = JsonConvert.DeserializeObject<EntityBase>(json);
            var token = JToken.Parse(json);
            if (entity.EntityType == "AppUser")
            {
                var userName = token["UserName"]?.Value<string>();
                if (userName == null)
                {
                    userName = token["Email"]?.Value<string>();
                    token["UserName"] = userName;
                }
                token["Key"] = NormalizeAlphaNumericKey(userName);
            }

            if (entity.EntityType == "Organization")
            {
                token["Key"] = token["Namespace"]?.Value<string>();
            }

            var nodes = EntityHeaderJson.FindEntityHeaderNodes(token);
            var wasUpdated = false;
            foreach (var node in nodes)
            {
                if (String.IsNullOrEmpty(node.Key) || String.IsNullOrEmpty(node.EntityType) &&
                    (node.EntityType != "AppUser" && !node.Path.EndsWith("OwnerOrganization") && !node.Path.EndsWith("CreatedBy") && !node.Path.EndsWith("LastUpdatedBy") && String.IsNullOrEmpty(node.OwnerOrgId)))
                {
                    wasUpdated = true;
                    var eh = await GetEntityHeaderForRecordAsync(node.Id);
                    if (eh != null)
                    {
                        if (eh.Id == NOT_FOUND_ID)
                        {
                            var childNode = await _nodeLocator.TryGetAsync(eh.Id, ct);
                            if (childNode == null)
                            {
                                EntityHeaderJson.SetResolved(node.Object, false);
                                if (!dryRun)
                                {
                                    if (String.IsNullOrEmpty(node.Key))
                                        await _fkWriter.AddOrphanedEHAsync(entity, node.NormalizedPath, EntityHeader.Create(node.Id, node.Text));
                                    else
                                        await _fkWriter.AddOrphanedEHAsync(entity, node.NormalizedPath, EntityHeader.Create(node.Id, node.Key, node.Text));
                                }
                                _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Unable to resolve EntityHeader for id {node.Id} referenced by entity {entity.Id}");
                            }
                            else
                            {
                                eh = await GetEntityHeaderForRecordAsync(childNode.RootId);
                                EntityHeaderJson.Update(node.Object, eh.Key, eh.Text, eh.OwnerOrgId, eh.IsPublic, eh.EntityType);
                            }
                        }
                        else
                            EntityHeaderJson.Update(node.Object, eh.Key, eh.Text, eh.OwnerOrgId, eh.IsPublic, eh.EntityType);
                    }
                }
            }

            _logger.Trace($"{this.Tag()} - Resolved {nodes.Count} entity header nodes for entity {entity.Id} of type {entity.EntityType}. Updated: {wasUpdated}");

            var fkNodes = ForeignKeyEdgeFactory.FromEntityHeaderNodes(entity, nodes);
            if (!dryRun)
                await _fkWriter.UpsertAllAsync(fkNodes);

            token["Sha256Hex"] = EntityHasher.CalculateHash(token.DeepClone());

            if (wasUpdated && !dryRun)
            {
                await UpsertJsonAsync(token.ToString());

                var result = new EhResolvedEntity()
                {
                    UpdatedEntity = wasUpdated,
                    Entity = entity,
                    EntityHeaderNodes = nodes,
                    ForeignKeyEdges = fkNodes.ToList(),
                    NotFoundEntityHeaderNodes = nodes.Where(n => String.IsNullOrEmpty(n.Key) || String.IsNullOrEmpty(n.EntityType)).ToList()
                };
                return InvokeResult<EhResolvedEntity>.Create(result);
            }
            else
            {
                var result = new EhResolvedEntity()
                {
                    UpdatedEntity = wasUpdated,
                    Entity = entity,
                    EntityHeaderNodes = nodes,
                    ForeignKeyEdges = fkNodes.ToList(),
                    NotFoundEntityHeaderNodes = nodes.Where(n => String.IsNullOrEmpty(n.Key) || String.IsNullOrEmpty(n.EntityType)).ToList()
                };
                return InvokeResult<EhResolvedEntity>.Create(result);
            }

        }

        public async Task<List<NodeLocatorEntry>> WriteNodesAsync(string id, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var json = await GetJsonByIdAsync(id);
            var entity = JsonConvert.DeserializeObject<EntityBase>(json);
            var token = JObject.Parse(json);
            if (entity.EntityType == null)
                return new List<NodeLocatorEntry>();

            var getMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart();

            var nodes = NodeLocatorWalker.ExtractNodeLocators(token, entity.OwnerOrganization?.Id ?? "SYSTEM", entity.EntityType, entity.Id, entity.Revision, entity.LastUpdatedDate);
            nodes = NodeLocatorTableWriterBatched.DeduplicateByNodeId(nodes, id);

            var dups = NodeLocatorTableWriterBatched.FindDuplicateNodeIds(nodes);
            if (dups.Count > 0)
            {
                Console.Error.WriteLine($"Found {dups.Count} duplicate NodeIds:");
                foreach (var dup in dups)
                {
                    Console.WriteLine($"RootId: {nodes.First().RootId}/{nodes.First().RootType} Count: {dup.Count} NodeType: {String.Join(',', dup.NodeTypes)}");
                    foreach (var path in dup.Paths)
                    {
                        Console.Write($"  Path: {path}");
                    }
                    Console.WriteLine();
                }
                Debugger.Break();
            }

            var extractDeDupMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            await _nodeLocatorWriter.UpsertAllAsync(nodes);

            var writeMs = sw.Elapsed.TotalMilliseconds;
            if (nodes.Count > 0)
                Console.WriteLine($"{nodes.First().RootType} - Node Count: {nodes.Count} - {getMs}/{extractDeDupMs}/{writeMs}");
            else
                Console.WriteLine("No node found ?!?!?!?!");

            return nodes;
        }

        public async Task<InvokeResult<List<EhResolvedEntity>>> ResolveEntityHeadersAsync(string entityType, string continuationToken, int pageSize = 100, int maxPagesThisRun = 10,
                                            CancellationToken ct = default, bool dryRun = false)
        {
            var results = new List<EhResolvedEntity>();

            await ScanContainerAsync(async (rec, ct) =>
            {
                var result = await ResolveEntityHeadersAsync(rec.Id, ct);
                results.Add(result.Result);

            }, continuationToken, entityType, pageSize, maxPagesThisRun);

            return InvokeResult<List<EhResolvedEntity>>.Create(results);
        }

        public async Task<InvokeResult<NodeLocatorResult>> AddNodeLocatorsAsync(string continuationToken, int pageSize = 100, int maxPagesThisRun = 10, CancellationToken ct = default, bool dryRun = false)
        {
            var result = new NodeLocatorResult();
            var recordIdx = 1;
            var continueToken = await ScanContainerAsync(async (rec, ct) =>
            {
                Console.Write($"{recordIdx++}/{pageSize * maxPagesThisRun} - ");
                var nodes = await WriteNodesAsync(rec.Id, ct);
                result.Entries.AddRange(nodes);


            }, continuationToken, pageSize: pageSize, maxPagesThisRun: maxPagesThisRun);

            result.ContinuationToken = continueToken;

            return InvokeResult<NodeLocatorResult>.Create(result);
        }

        public async Task<InvokeResult<EntityDeleteResult>> DeleteByEntityTypeAsync(string entityType, string continuationToken, bool dryRun, int pageSize = 100, int maxPagesThisRun = 10, CancellationToken ct = default)
        {
            var result = new EntityDeleteResult();

            var fullSw = Stopwatch.StartNew();
            result.ContinuationToken = await ScanContainerAsync(async (rec, ct) =>
            {
                if (dryRun)
                {
                    _logger.Trace($"{result.DeletedCount} - Would Delete {rec.Id} - {rec.EntityType}");
                }
                else
                {
                    var sw = Stopwatch.StartNew();
                    var deleteResult = await _container.DeleteItemAsync<EntityBase>(rec.Id, PartitionKey.None, cancellationToken: ct);
                    _logger.Trace($"{result.DeletedCount:0000} - Did Delete {rec.Id} - {rec.EntityType} in {sw.Elapsed.TotalMilliseconds}ms");
                    result.DeletedCount++;
                }

            }, continuationToken, entityType, pageSize, maxPagesThisRun);

            _logger.Trace($"Deleted {result.DeletedCount} in {fullSw.Elapsed.TotalMilliseconds} ms");

            return InvokeResult<EntityDeleteResult>.Create(result);
        }


        public const string ALL_MODULES_CACHE_KEY = "NUVIOT_ALL_MODULES";
        public const string MODULE_CACHE_KEY = "NUVIOT_MODULE_";

        public async Task<InvokeResult> PatchEntityAsync(PatchRequest request, EntityHeader org, EntityHeader user, CancellationToken ct = default)
        {
            _logger.Trace($"{this.Tag()} - Starting patch for entity {request.Id} of type {request.EntityType} with {request.Steps.Count} steps.");

            if (string.IsNullOrWhiteSpace(request.Id))
                return InvokeResult.FromError("id is required.");

            var ops = new List<PatchOperation>();

            foreach (var step in request.Steps)
            {
                switch (step.Op)
                {
                    case PatchOp.Remove:
                        ops.Add(PatchOperation.Remove(step.CosmosPath));
                        break;

                    case PatchOp.Set:
                        ops.Add(PatchOperation.Set(step.CosmosPath, step.Value));
                        break;
                    case PatchOp.Add:
                        ops.Add(PatchOperation.Add(step.CosmosPath, step.Value));
                        break;
                }
                _logger.Trace($"{this.Tag()} - Patch {step.Op} - {step.CosmosPath}.");

            }

            var options = new PatchItemRequestOptions
            {
                IfMatchEtag = request.ETag
            };

            if (ops.Count == 0)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(), "No operations for patch", request.Id.ToKVP("id"), request.EntityType.ToKVP("entityType"));
                return InvokeResult.FromError("No patch operations were provided.");
            }

            try
            {
                var patchResult = await _container.PatchItemAsync<JObject>(
                   id: request.Id,
                   partitionKey: request.PartitionKey == null ? PartitionKey.None : new PartitionKey(request.PartitionKey),
                   patchOperations: ops,
                   requestOptions: options,
                   cancellationToken: ct);

                _logger.Trace($"{this.Tag()} - Status Response: {patchResult.StatusCode} - Patched entity {request.Id} of type {request.EntityType} with {request.Steps.Count} steps.",
                    request.Id.ToKVP("id"),
                    request.EntityType.ToKVP("entityType"));

                if (patchResult.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), "Patch failed due to ETag mismatch (412 Precondition Failed).", request.Id.ToKVP("id"), request.EntityType.ToKVP("entityType"));
                    return InvokeResult.FromError("Patch failed due to ETag mismatch (412 Precondition Failed).");
                }

                await SetEntityHashAsync(request.Id);
                await _cacheProvider.RemoveAsync(GetCacheKey(request.EntityType, request.Id));
                if (request.EntityType == "Module")
                {
                    await _cacheProvider.RemoveAsync(ALL_MODULES_CACHE_KEY);
                    var json = await GetJsonByIdAsync(request.Id);
                    if (!String.IsNullOrEmpty(json))
                    {
                        var module = JsonConvert.DeserializeObject<EntityBase>(json);
                        await _cacheProvider.RemoveAsync($"{MODULE_CACHE_KEY}{module.Key}");
                    }
                }
                return InvokeResult.Success;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(),
                    "Patch failed due to ETag mismatch (412 Precondition Failed).",
                    request.Id.ToKVP("id"),
                    request.EntityType.ToKVP("entityType"));

                return InvokeResult.FromError("Patch failed due to ETag mismatch (412 Precondition Failed).");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(),
                    "Patch failed (404 NotFound) - item missing or wrong partition key.",
                    request.Id.ToKVP("id"),
                    (request.PartitionKey ?? request.Id).ToKVP("pk"),
                    request.EntityType.ToKVP("entityType"));

                return InvokeResult.FromError("Patch failed because the document was not found.");
            }
            catch (CosmosException ex) when (ex.StatusCode == (HttpStatusCode)429)
            {
                _logger.AddCustomEvent(LogLevel.Warning, this.Tag(),
                    "Patch throttled (429 TooManyRequests).",
                    request.Id.ToKVP("id"),
                    ex.RetryAfter.ToString().ToKVP("retryAfter"));

                return InvokeResult.FromError("Cosmos throttled the request. Please retry.");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(),
                    "Patch failed (400 BadRequest) - invalid patch operations.",
                    request.Id.ToKVP("id"),
                    request.EntityType.ToKVP("entityType"),
                    ex.Message.ToKVP("cosmosMessage"));

                return InvokeResult.FromError("Patch failed due to invalid patch operations.");
            }
        }
    }
}
