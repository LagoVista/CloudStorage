using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly ICacheProvider _cacheProvider;
        private readonly string _dbName;

        public const int DEFAULT_TAKE = 200;
        public const string FIXED_PARITIONKEY = null;

        public CosmosSyncRepository(ISyncConnectionSettings options, IFkIndexTableWriterBatched fkWriter, ICacheProvider cacheProvider, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fkWriter = fkWriter ?? throw new ArgumentNullException(nameof(fkWriter));
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
        public const string NOT_FOUND_KEY = "recordnotfound";
        public const string NOT_FOUND_TEXT = "Record Not Found";    
        public const string NOT_FOUND_ENTITYTYPE = "RecordNotFound";

        private Dictionary<string, EntityHeader> _inMemoryCache = new Dictionary<string, EntityHeader>();

        public async Task<EntityHeader> GetEntityHeaderForRecordAsync(string id, CancellationToken ct = default)
        {
            if(_inMemoryCache.ContainsKey(id))
            {
                return _inMemoryCache[id];
            }

            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));
            _logger.Trace($"{this.Tag()} - Request object for id {id}");
            var sql = @"SELECT c.id, c.Key, c.Name, c.Namespace, c.UserName, c.EntityType
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

            if(iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                var record = page.Resource?.FirstOrDefault();
                if(record != null)
                {
                    var eh = new EntityHeader()
                    {
                        Id = record.Id,
                        Key = record.GetKey(),
                        Text = record.Name,
                        EntityType = record.EntityType
                    };
                    _inMemoryCache.Add(eh.Id, eh);
                    return eh;
                }
            }

            var notFoundEh = new EntityHeader()
            {
                Id = NOT_FOUND_ID,
                Key = NOT_FOUND_KEY,
                Text = NOT_FOUND_TEXT,
                EntityType = NOT_FOUND_ENTITYTYPE
            };

            _inMemoryCache.Add(id, notFoundEh);

            return notFoundEh;
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
                "  c.IsDeleted, c.IsDeprecated, c.IsDraft, c.LastUpdatedDate " +
                "FROM c " +
                "WHERE c.EntityType = @entityType " +
                "  AND (IS_NULL(@search) OR @search = '' " +
                "       OR CONTAINS(LOWER(c.Name), @search) " +
                "       OR CONTAINS(LOWER(c.Key), @search)) " +
                "  AND c.OwnerOrganization.Id = @ownerOrganizationId " + 
                "ORDER BY c.key";


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
            if (string.IsNullOrWhiteSpace(id))
            {
                // Caller can omit; we generate.
                id = Guid.NewGuid().ToString("N");
                doc["id"] = id;
            }


            var entityType = doc["EntityType"]?.Value<string>()?.Trim();
            var key = doc["Key"]?.Value<string>()?.Trim();

            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("json must contain a non-empty 'entityType' property.", nameof(doc));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("json must contain a non-empty 'key' property.", nameof(doc));

            _logger.Trace($"{this.Tag()} - Apply", id.ToKVP("id"), key.ToKVP("key"), entityType.ToKVP("entityType"));

            // Ensure trimmed values are persisted.
            doc["EntityType"] = entityType;
            doc["Key"] = key;

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

                if(String.IsNullOrEmpty(existing))
                {
                    _logger.Trace($"{this.Tag()} - No matching record", id.ToKVP("id"), key.ToKVP("key"), entityType.ToKVP("entityType"));
                    doc[nameof(EntityBase.CreationDate)] = JToken.FromObject(user);
                    doc[nameof(EntityBase.CreatedBy)] = DateTime.UtcNow.ToJSONString();
                }
                else
                {
                    var entity = JsonConvert.DeserializeObject<EntityBase>(existing);
                    _logger.Trace($"{this.Tag()} - Found Exisitng Record", id.ToKVP("id"), key.ToKVP("key"), entityType.ToKVP("entityType"));
                    id = entity.Id;
                    doc["id"] = id;
                }

                doc[nameof(EntityBase.OwnerOrganization)] = JToken.FromObject(org);
                doc[nameof(EntityBase.LastUpdatedBy)] = JToken.FromObject(user);
                doc[nameof(EntityBase.LastUpdatedDate)] = DateTime.UtcNow.ToJSONString();

                await _cacheProvider.RemoveAsync(GetCacheKey(entityType, id));
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("json must be a valid JSON object.", nameof(json), ex);
            }

            return await UpsertJsonAsync(doc, null, ct);
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

            // Cosmos system property for concurrency/change detection
            [JsonProperty("_etag")]
            public string ETag { get; set; }
        }

        /// <summary>
        /// Scans a container using Cosmos paging and returns the continuation token to resume later.
        /// Persist the returned continuationToken somewhere durable.
        /// </summary>
        public async Task<string> ScanContainerAsync(Func<CosmosScanRow, CancellationToken, Task> handleRowAsync,
            string continuationToken = null, int pageSize = 100, int maxPagesThisRun = 10, string fixedPartitionKey = null, CancellationToken ct = default)
        {
            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = pageSize
            };

            if (!string.IsNullOrWhiteSpace(fixedPartitionKey))
            {
                requestOptions.PartitionKey = new PartitionKey(fixedPartitionKey);
            }

            // Minimal SELECT keeps RU and payload down. Add fields as needed.
            var qd = new QueryDefinition("SELECT c.id, c._etag FROM c");

            using var iterator = _container.GetItemQueryIterator<CosmosScanRow>(qd, continuationToken: continuationToken, requestOptions: requestOptions);
            var pagesRead = 0;

            while (iterator.HasMoreResults && pagesRead < maxPagesThisRun)
            {
                FeedResponse<CosmosScanRow> page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                pagesRead++;

                foreach (var row in page)
                {
                    await handleRowAsync(row, ct).ConfigureAwait(false);
                }

                // This is the real “resume from here” cursor.
                continuationToken = page.ContinuationToken;
            }

            return continuationToken; // null means you're done (or you started at end)
        }

        public async Task<InvokeResult<EhResolvedEntity>> ResolveEntityEntityHeadersAsync(string id, CancellationToken ct = default, bool dryRun = false)
        {
            var json = await GetJsonByIdAsync(id);
            var entity = JsonConvert.DeserializeObject<EntityBase>(json);
            var token = JToken.Parse(json);
            if(entity.EntityType == "AppUser")
            {
                var userName = token["UserName"]?.Value<string>();
                if (userName == null)
                {
                    userName = token["Email"]?.Value<string>();
                    token["UserName"] = userName;
                }
                token["Key"] = NormalizeAlphaNumericKey(userName);
            }

            var nodes = EntityHeaderJson.FindEntityHeaderNodes(token);
            var wasUpdated = false;
            foreach (var node in nodes)
            {
                if (String.IsNullOrEmpty(node.Key) || String.IsNullOrEmpty(node.EntityType) )
                {
                    wasUpdated = true;
                    var eh = await GetEntityHeaderForRecordAsync(node.Id);
                    if (eh != null)
                    {
                        if(eh.Id == NOT_FOUND_ID)
                        {
                            if (!dryRun)
                            {
                                if(String.IsNullOrEmpty(node.Key))
                                    await _fkWriter.AddOrphanedEHAsync(entity, node.NormalizedPath, EntityHeader.Create(node.Id, node.Text));
                                else 
                                    await _fkWriter.AddOrphanedEHAsync(entity, node.NormalizedPath, EntityHeader.Create(node.Id, node.Key, node.Text));
                            }
                            _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Unable to resolve EntityHeader for id {node.Id} referenced by entity {entity.Id}");
                        }
                        EntityHeaderJson.Update(node.Object, eh.Key, eh.Text, eh.EntityType);
                    }
                }
            }

            var fkNodes = ForeignKeyEdgeFactory.FromEntityHeaderNodes(entity, nodes);
            if(!dryRun)
                await _fkWriter.UpsertAllAsync(fkNodes);

            if(wasUpdated && !dryRun)
                await UpsertJsonAsync(token.ToString());

            var result = new EhResolvedEntity()
            {
                Entity = entity,
                EntityHeaderNodes = nodes,
                ForeignKeyEdges = fkNodes.ToList(),
                NotFoundEntityHeaderNodes = nodes.Where(n => String.IsNullOrEmpty(n.Key) || String.IsNullOrEmpty(n.EntityType)).ToList()
            };

            return InvokeResult<EhResolvedEntity>.Create(result);  
        }
    }
}
