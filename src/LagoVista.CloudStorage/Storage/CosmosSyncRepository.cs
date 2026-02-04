using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.PlatformSupport;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// =======================================================
// File: Sync/Repositories/Cosmos/CosmosSyncRepository.cs
// =======================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        public const int DEFAULT_TAKE = 200;
        public const string FIXED_PARITIONKEY = null;

        public CosmosSyncRepository(ISyncConnectionSettings options, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _client = new CosmosClient(_options.SyncConnectionSettings.Uri, _options.SyncConnectionSettings.AccessKey, new CosmosClientOptions
            {
                //SerializerOptions = new CosmosSerializationOptions
                //{
                //    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                //}
            });

            _container = _client.GetContainer(_options.SyncConnectionSettings.ResourceName, $"{_options.SyncConnectionSettings.ResourceName}_Collections");
        }


        public async Task<IReadOnlyList<SyncEntitySummary>> GetSummariesAsync(
                string entityType,
                string search = null,
                int take = 200,
                CancellationToken ct = default)
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
                "ORDER BY c.key";


            _logger.Trace($"{this.Tag()} - Query {sql}");

            var qd = new QueryDefinition(sql)
                .WithParameter("@entityType", entityType)
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
            var qd = new QueryDefinition(sql).WithParameter("@id", id.Trim());

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

        public async Task<SyncUpsertResult> UpsertJsonAsync(string json, string expectedETag = null, CancellationToken ct = default)
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

            // Validate minimum shape.
            var id = doc["id"]?.Value<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                // Caller can omit; we generate.
                id = Guid.NewGuid().ToString("N");
                doc["id"] = id;
            }

            var entityType = doc["entityType"]?.Value<string>()?.Trim();
            var key = doc["key"]?.Value<string>()?.Trim();

            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("json must contain a non-empty 'entityType' property.", nameof(json));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("json must contain a non-empty 'key' property.", nameof(json));

            // Ensure trimmed values are persisted.
            doc["entityType"] = entityType;
            doc["key"] = key;

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
                throw new InvalidOperationException("Upsert failed due to ETag mismatch (412 Precondition Failed).");
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = resp.Content != null ? await new StreamReader(resp.Content).ReadToEndAsync().ConfigureAwait(false) : null;
                throw new InvalidOperationException($"Upsert failed ({(int)resp.StatusCode} {resp.StatusCode}). {body}");
            }

            string returnedEtag = resp.Headers?.ETag;
            if (string.IsNullOrWhiteSpace(returnedEtag))
            {
                // Cosmos also includes _etag in the response body, but that requires parsing the stream again.
                // Header ETag is usually present; keep it optional.
                returnedEtag = null;
            }

            return new SyncUpsertResult
            {
                Id = id,
                ETag = returnedEtag,
                StatusCode = (int)resp.StatusCode,
                RequestCharge = resp.Headers?.RequestCharge
            };
        }
    }
}
