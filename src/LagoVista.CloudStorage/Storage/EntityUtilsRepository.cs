using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
namespace LagoVista.CloudStorage.Storage
{
    public class EntityUtilsRepository : IEntityUtilsRepository
    {
        private readonly IEntityDetailResponseFactory _entityDetailResponseFactory;
        private readonly CosmosClient _client;
        private readonly Container _container;
        private readonly ILogger _logger;
        private readonly ISyncConnectionSettings _options;
        private readonly ICacheProvider _cacheProvider;
        private readonly IRagIndexingServices _ragIndexingServices;
        public const string ALL_MODULES_CACHE_KEY = "NUVIOT_ALL_MODULES";
        public const string MODULE_CACHE_KEY = "NUVIOT_MODULE_";
        private readonly string _dbName;


        public EntityUtilsRepository(ISyncConnectionSettings options, IEntityDetailResponseFactory entityDetailResponseFactory, ICacheProvider cacheProvider, ILogger logger, IRagIndexingServices ragIndexingServices)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _entityDetailResponseFactory = entityDetailResponseFactory ?? throw new ArgumentNullException(nameof(entityDetailResponseFactory));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ragIndexingServices = ragIndexingServices ?? throw new ArgumentNullException(nameof(ragIndexingServices));
            _dbName = _options.SyncConnectionSettings.ResourceName;

            _client = new CosmosClient(_options.SyncConnectionSettings.Uri, _options.SyncConnectionSettings.AccessKey, new CosmosClientOptions
            {
            });

            _container = _client.GetContainer(_options.SyncConnectionSettings.ResourceName, $"{_options.SyncConnectionSettings.ResourceName}_Collections");
        }

        private string GetCacheKey(string entityType, string id)
        {
            return $"{_dbName}-{entityType}-{id}".ToLower();
        }

        public async Task<InvokeResult> CalculateHashAsync(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));

            // Query-by-id avoids needing partitionKey. Small datasets -> acceptable.
            const string sql = "SELECT * FROM c WHERE c.id = @id";
            var qd = new QueryDefinition(sql).WithParameter("@id", id.Trim());

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 1
            };

            using var iterator = _container.GetItemQueryIterator<JObject>(qd, requestOptions: requestOptions);

            JObject doc = null;

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                doc = page.Resource?.FirstOrDefault();
                if (doc == null)
                    return InvokeResult.FromError($"Could not find record with id: {id}");
            }

            var sha256Hex = EntityHasher.CalculateHash(doc);

            var sha256HexJsonPropertyName = nameof(IEntityBase.Sha256Hex);


            var patchOperations = new[]
            {
                PatchOperation.Set($"/{sha256HexJsonPropertyName}", sha256Hex)
            };

            await _container.PatchItemAsync<JObject>(id, partitionKey: PartitionKey.None, patchOperations: patchOperations,
                requestOptions: new PatchItemRequestOptions { EnableContentResponseOnWrite = false }, cancellationToken: ct).ConfigureAwait(false);

            var entityType = doc["EntityType"]?.Value<string>()?.Trim();
            var key = doc["Key"]?.Value<string>()?.Trim();

            await _cacheProvider.RemoveAsync(GetCacheKey(entityType, id));
            if (entityType == "Module")
            {
                await _cacheProvider.RemoveAsync(ALL_MODULES_CACHE_KEY);
                await _cacheProvider.RemoveAsync($"{MODULE_CACHE_KEY}{key}");
            }

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> IndexEntityAsync(string id, EntityHeader org, EntityHeader user, CancellationToken ct)
        {
            var modelResult = await _entityDetailResponseFactory.LoadModelAsync(id, user, org);
            if (modelResult.Model is IEntityBase model)
            {
                await _ragIndexingServices.IndexAsync(model);
                return InvokeResult.Success;
            }
            else
                return InvokeResult.FromError("not a entity");
        }
        public async Task<InvokeResult> PatchEntityFieldsAsync(string id, Dictionary<string, JToken> fields, EntityHeader user, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            if (!fields.Any()) return InvokeResult.Success;
            if (user == null) throw new ArgumentNullException(nameof(user));

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var doc = await LoadDocumentByIdAsync(id.Trim(), ct);

                if (doc == null)
                {
                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Could not find document with id '{id}' to patch entity fields.");
                    return InvokeResult.FromError($"Could not find record with id: {id}");
                }

                var now = UtcTimestamp.Now.Value;
                var patchOperations = new List<PatchOperation>();

                foreach (var field in fields)
                {
                    if (String.IsNullOrWhiteSpace(field.Key))
                        return InvokeResult.FromError("Could not patch entity because a field name was empty.");

                    patchOperations.Add(PatchOperation.Set($"/{field.Key}", field.Value ?? JValue.CreateNull()));
                }

                patchOperations.Add(PatchOperation.Set($"/{nameof(EntityBase.LastUpdatedDate)}", now));
                patchOperations.Add(PatchOperation.Set($"/{nameof(EntityBase.RevisionTimeStamp)}", now));

                if (doc[nameof(EntityBase.Revision)] != null)
                {
                    var currentRevision = doc[nameof(EntityBase.Revision)].Value<int>();
                    patchOperations.Add(PatchOperation.Set($"/{nameof(EntityBase.Revision)}", currentRevision + 1));
                }

                patchOperations.Add(PatchOperation.Set($"/{nameof(EntityBase.LastUpdatedBy)}", JObject.FromObject(user)));

                var etag = doc["_etag"]?.Value<string>();

                try
                {
                    await _container.PatchItemAsync<JObject>(id.Trim(), PartitionKey.None, patchOperations,
                        requestOptions: new PatchItemRequestOptions { EnableContentResponseOnWrite = false, IfMatchEtag = etag }, cancellationToken: ct).ConfigureAwait(false);

                    _logger.Trace($"{this.Tag()} - Patched fields [{String.Join(", ", fields.Keys)}] for entity '{id}' on attempt {attempt}.");
                    await RemoveEntityCachesAsync(doc);
                    return InvokeResult.Success;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed && attempt < 3)
                {
                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"ETag conflict while patching fields for entity '{id}'. Retrying attempt {attempt + 1}.");
                }
            }

            return InvokeResult.FromError($"Could not patch fields for entity '{id}' because the document was updated concurrently.");
        }


        public async Task<InvokeResult> UpsertAiEntitySessionAsync(string id, AiEntitySession session, CancellationToken ct)
        {
            _logger.Trace($"{this.Tag()} - Upserted AI entity session for entity '{id}' with session {session.Session.Id} and key {session.SessionType}/{session.SessionTypeKey}.");

            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (session.Session == null || String.IsNullOrWhiteSpace(session.Session.Id)) throw new ArgumentException("session.Session is required.", nameof(session));
            if (String.IsNullOrWhiteSpace(session.SessionType)) throw new ArgumentException("session.SessionType is required.", nameof(session));
            if (String.IsNullOrWhiteSpace(session.SessionTypeKey)) throw new ArgumentException("session.SessionTypeKey is required.", nameof(session));

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var doc = await LoadDocumentByIdAsync(id.Trim(), ct);

                if (doc == null)
                {
                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Could not find document with id '{id}' to upsert AI entity session.");
                    return InvokeResult.FromError($"Could not find record with id: {id}");
                }

                var sessions = ReadAiEntitySessions(doc);
                var existing = sessions.SingleOrDefault(item => String.Equals(item.SessionType, session.SessionType, StringComparison.OrdinalIgnoreCase) && String.Equals(item.SessionTypeKey, session.SessionTypeKey, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    sessions.Add(session);
                }
                else
                {
                    existing.Session = session.Session;
                    existing.SessionType = session.SessionType;
                    existing.SessionTypeKey = session.SessionTypeKey;
                    existing.CreationDate = session.CreationDate;
                    existing.CreatedBy = session.CreatedBy;
                }

                var etag = doc["_etag"]?.Value<string>();
                var patchOperations = new[]
                {
                    PatchOperation.Set($"/{nameof(EntityBase.AiEntitySessions)}", JArray.FromObject(sessions))
                };

                try
                {
                    await _container.PatchItemAsync<JObject>(id.Trim(), PartitionKey.None, patchOperations,
                        requestOptions: new PatchItemRequestOptions { EnableContentResponseOnWrite = false, IfMatchEtag = etag }, cancellationToken: ct).ConfigureAwait(false);
                    _logger.Trace($"{this.Tag()} - Upserted AI entity session for entity '{id}' on attempt {attempt}.");
                    await RemoveEntityCachesAsync(doc);
                    return InvokeResult.Success;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed && attempt < 3)
                {
                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"ETag conflict while upserting AI entity session for entity '{id}'. Retrying attempt {attempt + 1}.");
                }
            }

            return InvokeResult.FromError($"Could not upsert AI entity session for entity '{id}' because the document was updated concurrently.");
        }

        private async Task<JObject> LoadDocumentByIdAsync(string id, CancellationToken ct)
        {
            const string sql = "SELECT * FROM c WHERE c.id = @id";
            var qd = new QueryDefinition(sql).WithParameter("@id", id);

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 1
            };

            using var iterator = _container.GetItemQueryIterator<JObject>(qd, requestOptions: requestOptions);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                var doc = page.Resource?.FirstOrDefault();

                if (doc != null)
                    return doc;
            }

            return null;
        }

        private static List<AiEntitySession> ReadAiEntitySessions(JObject doc)
        {
            var sessionsToken = doc[nameof(EntityBase.AiEntitySessions)];

            if (sessionsToken == null || sessionsToken.Type == JTokenType.Null)
                return new List<AiEntitySession>();

            if (sessionsToken.Type != JTokenType.Array)
                throw new InvalidOperationException($"Could not update AI entity sessions because {nameof(EntityBase.AiEntitySessions)} was not an array.");

            return sessionsToken.ToObject<List<AiEntitySession>>() ?? new List<AiEntitySession>();
        }

        private async Task RemoveEntityCachesAsync(JObject doc)
        {
            var entityType = doc["EntityType"]?.Value<string>()?.Trim();
            var key = doc["Key"]?.Value<string>()?.Trim();
            var id = doc["id"]?.Value<string>()?.Trim();

            if (String.IsNullOrWhiteSpace(entityType) || String.IsNullOrWhiteSpace(id))
                return;

            await _cacheProvider.RemoveAsync(GetCacheKey(entityType, id));

            if (entityType == "Module")
            {
                await _cacheProvider.RemoveAsync(ALL_MODULES_CACHE_KEY);
                await _cacheProvider.RemoveAsync($"{MODULE_CACHE_KEY}{key}");
            }
        }
    }
}
