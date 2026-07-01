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

        public async Task<List<EntityBaseSummary>> GetEntityBasesAsync(string entityType, EntityHeader org)
        {
            if (String.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("entityType is required.", nameof(entityType));

            if (org == null)
                throw new ArgumentNullException(nameof(org));

            if (String.IsNullOrWhiteSpace(org.Id))
                throw new ArgumentException("org.Id is required.", nameof(org));

            var result = await GetEntitiesByTypeAsync(entityType.Trim(), org.Id.Trim(), CancellationToken.None).ConfigureAwait(false);

            if (!result.Successful)
                throw new InvalidOperationException($"Could not retrieve entities of type '{entityType}'.");

            var entities = new List<EntityBaseSummary>();

            foreach (var document in result.Result ?? new List<JObject>())
            {
                var entity = document.ToObject<EntityBaseSummary>();
                if (entity == null)
                {
                    var entityId = document["id"]?.Value<string>() ?? "unknown";

                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Could not deserialize entity '{entityId}' as {nameof(EntityBaseSummary)}.");

                    throw new InvalidOperationException($"Could not deserialize entity '{entityId}' as {nameof(EntityBaseSummary)}.");
                }

                entities.Add(entity);
            }

            return entities;
        }

        public Task<InvokeResult> PatchMasterStatusAsync(string id, MasterEntityStatus masterStatus, EntityHeader user, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id is required.", nameof(id));

            if (masterStatus == null)
                throw new ArgumentNullException(nameof(masterStatus));

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var fields = new Dictionary<string, JToken>
            {
                [nameof(IEntityBase.MasterStatus)] = JObject.FromObject(masterStatus)
            };

            return PatchEntityFieldsAsync(id, fields, user, ct);
        }

        public async Task<EntityBase> GetEntityBaseAsync(string id, EntityHeader org)
        {
            if (String.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id is required.", nameof(id));

            if (org == null)
                throw new ArgumentNullException(nameof(org));

            if (String.IsNullOrWhiteSpace(org.Id))
                throw new ArgumentException("org.Id is required.", nameof(org));

            var document = await LoadDocumentByIdAsync(id.Trim(), CancellationToken.None).ConfigureAwait(false);

            if (document == null)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Could not find document with id '{id}'.");
                throw new KeyNotFoundException($"Could not find record with id: {id}");
            }

            var ownerOrganizationId = document[nameof(EntityBase.OwnerOrganization)]?["Id"]?.Value<string>();

            if (!String.Equals(ownerOrganizationId, org.Id, StringComparison.OrdinalIgnoreCase))
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Entity '{id}' is owned by organization '{ownerOrganizationId ?? "unknown"}' rather than '{org.Id}'.");

                throw new UnauthorizedAccessException($"Entity '{id}' does not belong to organization '{org.Id}'.");
            }

            var entity = document.ToObject<EntityBase>();

            if (entity == null)
                throw new InvalidOperationException($"Could not deserialize entity '{id}' as {nameof(EntityBase)}.");

            return entity;
        }

        public async Task<InvokeResult<List<JObject>>> GetEntityReadinessScorecardCandidatesAsync(IEnumerable<string> entityTypes, string orgId, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("orgId is required.", nameof(orgId));

            var requestedEntityTypes = (entityTypes ?? Enumerable.Empty<string>())
                .Where(entityType => !String.IsNullOrWhiteSpace(entityType))
                .Select(entityType => entityType.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedEntityTypes.Count == 0)
            {
                return InvokeResult<List<JObject>>.Create(new List<JObject>());
            }

            const string sql =
        @"SELECT
    c.id,
    c.EntityType,
    c.ReadinessChecks
FROM c
WHERE c.OwnerOrganization.Id = @orgId
AND ARRAY_CONTAINS(@entityTypes, c.EntityType)";

            var query = new QueryDefinition(sql)
                .WithParameter("@orgId", orgId.Trim())
                .WithParameter("@entityTypes", requestedEntityTypes);

            var results = new List<JObject>();
            var requestOptions = new QueryRequestOptions { MaxItemCount = 100 };

            try
            {
                using var iterator = _container.GetItemQueryIterator<JObject>(query, requestOptions: requestOptions);

                while (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                    results.AddRange(page.Resource.Where(item => item != null));
                }

                _logger.Trace($"{this.Tag()} - Found {results.Count} readiness scorecard entities across {requestedEntityTypes.Count} entity types for organization '{orgId}'.");

                return InvokeResult<List<JObject>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                return InvokeResult<List<JObject>>.FromException(this.Tag(), ex);
            }
        }

        public async Task<InvokeResult<List<JObject>>> GetEntityReadinessCandidatesAsync(string entityType, string orgId, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("entityType is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("orgId is required.", nameof(orgId));

            const string sql =
        @"SELECT
    c.id,
    c.EntityType,
    c.ChecklistStatus,
    c.ReadinessChecks,
    c._etag
FROM c
WHERE c.OwnerOrganization.Id = @orgId
AND c.EntityType = @entityType";

            var query = new QueryDefinition(sql)
                .WithParameter("@orgId", orgId.Trim())
                .WithParameter("@entityType", entityType.Trim());

            var results = new List<JObject>();
            var requestOptions = new QueryRequestOptions { MaxItemCount = 100 };

            try
            {
                using var iterator = _container.GetItemQueryIterator<JObject>(query, requestOptions: requestOptions);

                while (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                    results.AddRange(page.Resource.Where(item => item != null));
                }

                _logger.Trace($"{this.Tag()} - Found {results.Count} readiness reconciliation candidates for entity type '{entityType}' in organization '{orgId}'.");

                return InvokeResult<List<JObject>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                return InvokeResult<List<JObject>>.FromException(this.Tag(), ex);
            }
        }


        public async Task<InvokeResult<List<JObject>>> GetEntitiesWithEmptyFieldAsync(
    string entityType,
    string fieldName,
    string orgId,
    int maxItems,
    CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("entityType is required.", nameof(entityType));
            }

            if (String.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("fieldName is required.", nameof(fieldName));
            }

            if (String.IsNullOrWhiteSpace(orgId))
            {
                throw new ArgumentException("orgId is required.", nameof(orgId));
            }

            if (maxItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be greater than zero.");
            }

            var safeFieldName = fieldName.Trim();

            if (!IsSafeCosmosPropertyName(safeFieldName))
            {
                return InvokeResult<List<JObject>>.FromError($"Field name '{fieldName}' is not safe for a Cosmos query.");
            }

            var results = new List<JObject>();

            var sql =
        $@"SELECT TOP {maxItems} *
FROM c
WHERE c.EntityType = @entityType
AND c.OwnerOrganization.Id = @orgId
AND (
    NOT IS_DEFINED(c[""{safeFieldName}""])
    OR IS_NULL(c[""{safeFieldName}""])
    OR c[""{safeFieldName}""] = ''
)";

            var qd = new QueryDefinition(sql)
                .WithParameter("@entityType", entityType.Trim())
                .WithParameter("@orgId", orgId.Trim());

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = Math.Min(maxItems, 100)
            };

            try
            {
                using var iterator = _container.GetItemQueryIterator<JObject>(
                    qd,
                    requestOptions: requestOptions);

                while (iterator.HasMoreResults && results.Count < maxItems)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);

                    foreach (var doc in page.Resource)
                    {
                        if (doc == null)
                        {
                            continue;
                        }

                        results.Add(doc);

                        if (results.Count >= maxItems)
                        {
                            break;
                        }
                    }
                }

                _logger.Trace($"{this.Tag()} - Found {results.Count} {entityType} entities with empty field '{safeFieldName}'.");

                return InvokeResult<List<JObject>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                return InvokeResult<List<JObject>>.FromException(this.Tag(), ex);
            }
        }

        private static bool IsSafeCosmosPropertyName(string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (fieldName.Length > 128)
            {
                return false;
            }

            foreach (var ch in fieldName)
            {
                if (!(Char.IsLetterOrDigit(ch) || ch == '_'))
                {
                    return false;
                }
            }

            return true;
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

            const int maxAttempts = 5;
            var documentId = id.Trim();

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                var doc = await LoadDocumentByIdAsync(documentId, ct).ConfigureAwait(false);

                if (doc == null)
                {
                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Could not find document with id '{documentId}' to patch entity fields.");
                    return InvokeResult.FromError($"Could not find record with id: {documentId}");
                }

                var etag = doc["_etag"]?.Value<string>();

                if (String.IsNullOrWhiteSpace(etag))
                {
                    _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Could not patch entity '{documentId}' because its ETag was missing.");
                    return InvokeResult.FromError($"Could not patch entity '{documentId}' because its ETag was missing.");
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

                try
                {
                    await _container.PatchItemAsync<JObject>(
                        documentId,
                        PartitionKey.None,
                        patchOperations,
                        requestOptions: new PatchItemRequestOptions
                        {
                            EnableContentResponseOnWrite = false,
                            IfMatchEtag = etag
                        },
                        cancellationToken: ct).ConfigureAwait(false);

                    _logger.Trace($"{this.Tag()} - Patched fields [{String.Join(", ", fields.Keys)}] for entity '{documentId}' on attempt {attempt}.");

                    await RemoveEntityCachesAsync(doc).ConfigureAwait(false);

                    return InvokeResult.Success;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    if (attempt == maxAttempts)
                    {
                        _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"ETag conflict while patching fields for entity '{documentId}'. All {maxAttempts} attempts failed.");

                        return InvokeResult.FromError(
                            $"Could not patch fields for entity '{documentId}' because the document was updated concurrently.");
                    }

                    var exponentialDelayMs = 50 * (1 << (attempt - 1));
                    var jitterMs = new Random().Next(0, 50);
                    var delay = TimeSpan.FromMilliseconds(exponentialDelayMs + jitterMs);

                    _logger.AddCustomEvent(
                        LogLevel.Warning,
                        this.Tag(),
                        $"ETag conflict while patching fields for entity '{documentId}'. Retrying attempt {attempt + 1} after {delay.TotalMilliseconds:0} ms.");

                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }

            return InvokeResult.FromError($"Could not patch fields for entity '{documentId}' because the document was updated concurrently.");
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

        public async Task<JObject> GetEntityByIdAsync(string entityType, string entityId, string orgId, CancellationToken token)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("Entity type is required.", nameof(entityType));
            }

            if (String.IsNullOrWhiteSpace(entityId))
            {
                throw new ArgumentException("Entity id is required.", nameof(entityId));
            }

            if (String.IsNullOrWhiteSpace(orgId))
            {
                throw new ArgumentException("Organization id is required.", nameof(orgId));
            }

            var doc = await LoadDocumentByIdAsync(entityId.Trim(), token);

            if (doc == null)
            {
                return null;
            }

            var owner = doc[nameof(EntityBase.OwnerOrganization)]?.ToObject<EntityHeader>();

            if (owner == null || String.IsNullOrWhiteSpace(owner.Id))
            {
                throw new InvalidOperationException("Owner organization is not present; ownership could not be verified.");
            }

            if (!String.Equals(owner.Id, orgId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The entity does not belong to the specified organization.");
            }

            var storedEntityType = doc[nameof(EntityBase.EntityType)]?.Value<string>();

            if (!String.Equals(storedEntityType, entityType.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Entity type mismatch. Expected '{entityType}', but found '{storedEntityType}'.");
            }

            return doc;
        }

        public async Task<InvokeResult<Dictionary<string, JToken>>> GetEntityFieldsAsync(string id, IEnumerable<string> fieldNames, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("id is required.", nameof(id));
            }

            if (fieldNames == null)
            {
                throw new ArgumentNullException(nameof(fieldNames));
            }

            var requestedFields = fieldNames
                .Where(fieldName => !String.IsNullOrWhiteSpace(fieldName))
                .Select(fieldName => fieldName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!requestedFields.Any())
            {
                return InvokeResult<Dictionary<string, JToken>>.Create(new Dictionary<string, JToken>());
            }

            var doc = await LoadDocumentByIdAsync(id.Trim(), ct).ConfigureAwait(false);

            if (doc == null)
            {
                _logger.AddCustomEvent(LogLevel.Error, this.Tag(), $"Could not find document with id '{id}' to read entity fields.");
                return InvokeResult<Dictionary<string, JToken>>.FromError($"Could not find record with id: {id}");
            }

            var result = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

            foreach (var fieldName in requestedFields)
            {
                result[fieldName] = doc.TryGetValue(fieldName, StringComparison.OrdinalIgnoreCase, out var token)
                    ? token
                    : JValue.CreateNull();
            }

            return InvokeResult<Dictionary<string, JToken>>.Create(result);
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

        public Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, int maxItems, CancellationToken ct)
        {
            return GetEntitiesWithCompletedChecklistStepsInternalAsync(entityType, orgId, checklistStepKeys, null, maxItems, ct);
        }

        public Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, string targetChecklistStepKey, int maxItems, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(targetChecklistStepKey))
            {
                throw new ArgumentException("targetChecklistStepKey is required.", nameof(targetChecklistStepKey));
            }

            return GetEntitiesWithCompletedChecklistStepsInternalAsync(entityType, orgId, checklistStepKeys, targetChecklistStepKey.Trim(), maxItems, ct);
        }

        public Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<EntityChecklistStep> checklistSteps, int maxItems, CancellationToken ct)
        {
            if (checklistSteps == null)
            {
                throw new ArgumentNullException(nameof(checklistSteps));
            }

            var checklistStepKeys = checklistSteps
                .Where(step => step != null && !String.IsNullOrWhiteSpace(step.Key))
                .Select(step => step.Key)
                .ToList();

            return GetEntitiesWithCompletedChecklistStepsAsync(entityType, orgId, checklistStepKeys, maxItems, ct);
        }

        private async Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsInternalAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, string targetChecklistStepKey, int maxItems, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("entityType is required.", nameof(entityType));
            }

            if (String.IsNullOrWhiteSpace(orgId))
            {
                throw new ArgumentException("orgId is required.", nameof(orgId));
            }

            if (checklistStepKeys == null)
            {
                throw new ArgumentNullException(nameof(checklistStepKeys));
            }

            if (maxItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be greater than zero.");
            }

            var stepKeys = NormalizeChecklistStepKeys(checklistStepKeys);

            if (!stepKeys.Any())
            {
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromError("At least one checklist step key is required.");
            }

            var validation = ValidateChecklistStepKeys(stepKeys);

            if (!validation.Successful)
            {
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromInvokeResult(validation);
            }

            var take = Math.Min(maxItems, 5000);
            var predicates = new List<string>
    {
        "c.EntityType = @entityType",
        "c.OwnerOrganization.Id = @orgId"
    };

            for (var idx = 0; idx < stepKeys.Count; idx++)
            {
                predicates.Add(BuildCompletedChecklistStepPredicate($"@stepKey{idx}"));
            }

            var sql = BuildCandidateSummarySql(predicates, take);

            var qd = new QueryDefinition(sql)
                .WithParameter("@entityType", entityType.Trim())
                .WithParameter("@orgId", orgId.Trim())
                .WithParameter("@completedStatus", EntityChecklistStatus.Completed);

            for (var idx = 0; idx < stepKeys.Count; idx++)
            {
                qd = qd.WithParameter($"@stepKey{idx}", stepKeys[idx]);
            }

            _logger.Trace($"{this.Tag()} - Finding completed check lists", sql.ToKVP("query"));

            return await ExecuteCandidateSummaryQueryAsync(
                qd,
                stepKeys,
                targetChecklistStepKey,
                take,
                $"{this.Tag()} - Found completed checklist candidates for {entityType} with steps [{String.Join(", ", stepKeys)}].",
                ct).ConfigureAwait(false);
        }


        public Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, string targetIncompleteStepKey, int maxItems, CancellationToken ct)
        {
            var targetIncompleteStepKeys = String.IsNullOrWhiteSpace(targetIncompleteStepKey) ? Enumerable.Empty<string>() : new[] { targetIncompleteStepKey };

            return GetEntitiesReadyForChecklistStepAsync(entityType, orgId, requiredCompletedStepKeys, targetIncompleteStepKeys, maxItems, ct);
        }

        public async Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, IEnumerable<string> targetIncompleteStepKeys, int maxItems, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("entityType is required.", nameof(entityType));
            }

            if (String.IsNullOrWhiteSpace(orgId))
            {
                throw new ArgumentException("orgId is required.", nameof(orgId));
            }

            if (requiredCompletedStepKeys == null)
            {
                throw new ArgumentNullException(nameof(requiredCompletedStepKeys));
            }

            if (targetIncompleteStepKeys == null)
            {
                throw new ArgumentNullException(nameof(targetIncompleteStepKeys));
            }

            if (maxItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be greater than zero.");
            }

            var requiredStepKeys = NormalizeChecklistStepKeys(requiredCompletedStepKeys);
            var targetStepKeys = NormalizeChecklistStepKeys(targetIncompleteStepKeys);

            if (!targetStepKeys.Any())
            {
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromError("At least one target incomplete checklist step key is required.");
            }

            var requiredValidation = ValidateChecklistStepKeys(requiredStepKeys);
            if (!requiredValidation.Successful)
            {
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromInvokeResult(requiredValidation);
            }

            var targetValidation = ValidateChecklistStepKeys(targetStepKeys);
            if (!targetValidation.Successful)
            {
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromInvokeResult(targetValidation);
            }

            var take = Math.Min(maxItems, 5000);
            var predicates = new List<string> { "c.EntityType = @entityType", "c.OwnerOrganization.Id = @orgId" };

            for (var idx = 0; idx < requiredStepKeys.Count; idx++)
            {
                predicates.Add(BuildCompletedChecklistStepPredicate($"@requiredStepKey{idx}"));
            }

            var incompleteTargetPredicates = new List<string>();

            for (var idx = 0; idx < targetStepKeys.Count; idx++)
            {
                incompleteTargetPredicates.Add(BuildIncompleteChecklistStepPredicate($"@targetStepKey{idx}"));
            }

            predicates.Add($"({String.Join(" OR ", incompleteTargetPredicates)})");

            var sql = BuildCandidateSummarySql(predicates, take);
            var qd = new QueryDefinition(sql).WithParameter("@entityType", entityType.Trim()).WithParameter("@orgId", orgId.Trim()).WithParameter("@completedStatus", EntityChecklistStatus.Completed);

            _logger.Trace($"{this.Tag()} - Finding completed check lists", sql.ToKVP("query"));

            for (var idx = 0; idx < requiredStepKeys.Count; idx++)
            {
                qd = qd.WithParameter($"@requiredStepKey{idx}", requiredStepKeys[idx]);
            }

            for (var idx = 0; idx < targetStepKeys.Count; idx++)
            {
                qd = qd.WithParameter($"@targetStepKey{idx}", targetStepKeys[idx]);
            }

            return await ExecuteCandidateSummaryQueryAsync(qd, targetStepKeys, null, take, $"{this.Tag()} - Found ready checklist candidates for {entityType} with prerequisites [{String.Join(", ", requiredStepKeys)}] and targets [{String.Join(", ", targetStepKeys)}].", ct).ConfigureAwait(false);
        }

        public async Task<InvokeResult<int>> CountEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct)
        {
            const string tag = "[EntityUtilsRepository__CountEntitiesByTypeAsync]";

            try
            {
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    return InvokeResult<int>.FromError("Entity type is required.");
                }

                if (String.IsNullOrWhiteSpace(orgId))
                {
                    return InvokeResult<int>.FromError("Organization id is required.");
                }

                var qd = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.EntityType = @entityType AND c.OwnerOrganization.Id = @orgId").WithParameter("@entityType", entityType.Trim()).WithParameter("@orgId", orgId.Trim());
                var count = await ExecuteScalarIntAsync(qd, ct).ConfigureAwait(false);

                return InvokeResult<int>.Create(count);
            }
            catch (Exception ex)
            {
                _logger.AddException(tag, ex);
                return InvokeResult<int>.FromException(tag, ex);
            }
        }

        public async Task<InvokeResult<int>> CountEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, CancellationToken ct)
        {
            const string tag = "[EntityUtilsRepository__CountEntitiesWithCompletedChecklistStepsAsync]";

            try
            {
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    return InvokeResult<int>.FromError("Entity type is required.");
                }

                if (String.IsNullOrWhiteSpace(orgId))
                {
                    return InvokeResult<int>.FromError("Organization id is required.");
                }

                if (checklistStepKeys == null)
                {
                    throw new ArgumentNullException(nameof(checklistStepKeys));
                }

                var stepKeys = NormalizeChecklistStepKeys(checklistStepKeys);

                if (!stepKeys.Any())
                {
                    return InvokeResult<int>.FromError("At least one completed checklist step key is required.");
                }

                var validation = ValidateChecklistStepKeys(stepKeys);
                if (!validation.Successful)
                {
                    return InvokeResult<int>.FromInvokeResult(validation);
                }

                var predicates = new List<string> { "c.EntityType = @entityType", "c.OwnerOrganization.Id = @orgId" };

                for (var idx = 0; idx < stepKeys.Count; idx++)
                {
                    predicates.Add(BuildCompletedChecklistStepPredicate($"@stepKey{idx}"));
                }

                var sql = $"SELECT VALUE COUNT(1) FROM c WHERE {String.Join(" AND ", predicates)}";
                var qd = new QueryDefinition(sql).WithParameter("@entityType", entityType.Trim()).WithParameter("@orgId", orgId.Trim()).WithParameter("@completedStatus", EntityChecklistStatus.Completed);

                _logger.Trace($"{this.Tag()} - Finding completed check lists", sql.ToKVP("query"));

                for (var idx = 0; idx < stepKeys.Count; idx++)
                {
                    qd = qd.WithParameter($"@stepKey{idx}", stepKeys[idx]);
                }

                var count = await ExecuteScalarIntAsync(qd, ct).ConfigureAwait(false);

                return InvokeResult<int>.Create(count);
            }
            catch (Exception ex)
            {
                _logger.AddException(tag, ex);
                return InvokeResult<int>.FromException(tag, ex);
            }
        }

        public Task<InvokeResult<int>> CountEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, string targetIncompleteStepKey, CancellationToken ct)
        {
            var targetIncompleteStepKeys = String.IsNullOrWhiteSpace(targetIncompleteStepKey) ? Enumerable.Empty<string>() : new[] { targetIncompleteStepKey };

            return CountEntitiesReadyForChecklistStepAsync(entityType, orgId, requiredCompletedStepKeys, targetIncompleteStepKeys, ct);
        }

        public async Task<InvokeResult<int>> CountEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, IEnumerable<string> targetIncompleteStepKeys, CancellationToken ct)
        {
            const string tag = "[EntityUtilsRepository__CountEntitiesReadyForChecklistStepAsync]";

            try
            {
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    return InvokeResult<int>.FromError("Entity type is required.");
                }

                if (String.IsNullOrWhiteSpace(orgId))
                {
                    return InvokeResult<int>.FromError("Organization id is required.");
                }

                if (requiredCompletedStepKeys == null)
                {
                    throw new ArgumentNullException(nameof(requiredCompletedStepKeys));
                }

                if (targetIncompleteStepKeys == null)
                {
                    throw new ArgumentNullException(nameof(targetIncompleteStepKeys));
                }

                var requiredStepKeys = NormalizeChecklistStepKeys(requiredCompletedStepKeys);
                var targetStepKeys = NormalizeChecklistStepKeys(targetIncompleteStepKeys);

                if (!targetStepKeys.Any())
                {
                    return InvokeResult<int>.FromError("At least one target incomplete checklist step key is required.");
                }

                var requiredValidation = ValidateChecklistStepKeys(requiredStepKeys);
                if (!requiredValidation.Successful)
                {
                    return InvokeResult<int>.FromInvokeResult(requiredValidation);
                }

                var targetValidation = ValidateChecklistStepKeys(targetStepKeys);
                if (!targetValidation.Successful)
                {
                    return InvokeResult<int>.FromInvokeResult(targetValidation);
                }

                var predicates = new List<string> { "c.EntityType = @entityType", "c.OwnerOrganization.Id = @orgId" };

                for (var idx = 0; idx < requiredStepKeys.Count; idx++)
                {
                    predicates.Add(BuildCompletedChecklistStepPredicate($"@requiredStepKey{idx}"));
                }

                var incompleteTargetPredicates = new List<string>();

                for (var idx = 0; idx < targetStepKeys.Count; idx++)
                {
                    incompleteTargetPredicates.Add(BuildIncompleteChecklistStepPredicate($"@targetStepKey{idx}"));
                }

                predicates.Add($"({String.Join(" OR ", incompleteTargetPredicates)})");

                var sql = $"SELECT VALUE COUNT(1) FROM c WHERE {String.Join(" AND ", predicates)}";
                var qd = new QueryDefinition(sql).WithParameter("@entityType", entityType.Trim()).WithParameter("@orgId", orgId.Trim()).WithParameter("@completedStatus", EntityChecklistStatus.Completed);

                for (var idx = 0; idx < requiredStepKeys.Count; idx++)
                {
                    qd = qd.WithParameter($"@requiredStepKey{idx}", requiredStepKeys[idx]);
                }

                for (var idx = 0; idx < targetStepKeys.Count; idx++)
                {
                    qd = qd.WithParameter($"@targetStepKey{idx}", targetStepKeys[idx]);
                }

                var count = await ExecuteScalarIntAsync(qd, ct).ConfigureAwait(false);

                return InvokeResult<int>.Create(count);
            }
            catch (Exception ex)
            {
                _logger.AddException(tag, ex);
                return InvokeResult<int>.FromException(tag, ex);
            }
        }

        private async Task<InvokeResult<List<EntityChecklistCandidateSummary>>> ExecuteCandidateSummaryQueryAsync(QueryDefinition queryDefinition, IReadOnlyCollection<string> checklistStepKeys, string targetChecklistStepKey, int maxItems, string successMessage, CancellationToken ct)
        {
            if (queryDefinition == null)
            {
                throw new ArgumentNullException(nameof(queryDefinition));
            }

            var documents = new List<EntityChecklistCandidateDocument>();
            var requestOptions = new QueryRequestOptions { MaxItemCount = Math.Min(maxItems, 100) };
            var normalizedChecklistStepKeys = NormalizeChecklistStepKeys(checklistStepKeys ?? Enumerable.Empty<string>());
            var checklistStepKeySet = normalizedChecklistStepKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var iterator = _container.GetItemQueryIterator<EntityChecklistCandidateDocument>(queryDefinition, requestOptions: requestOptions);

                while (iterator.HasMoreResults && documents.Count < maxItems)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);

                    foreach (var document in page.Resource)
                    {
                        if (document == null)
                        {
                            continue;
                        }

                        documents.Add(document);

                        if (documents.Count >= maxItems)
                        {
                            break;
                        }
                    }
                }

                var results = documents
                    .Select(document =>
                    {
                        var checklistStatus = document.ChecklistStatus ?? new List<EntityChecklistStatus>();

                        var completedTargetStepKeys = checklistStatus
                            .Where(status =>
                                status != null &&
                                !String.IsNullOrWhiteSpace(status.StepKey) &&
                                checklistStepKeySet.Contains(status.StepKey) &&
                                status.Status != null &&
                                String.Equals(status.Status.Key, EntityChecklistStatus.Completed, StringComparison.OrdinalIgnoreCase))
                            .Select(status => status.StepKey)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var targetChecklistStatus = String.IsNullOrWhiteSpace(targetChecklistStepKey)
                            ? null
                            : checklistStatus.FirstOrDefault(status =>
                                status != null &&
                                String.Equals(status.StepKey, targetChecklistStepKey, StringComparison.OrdinalIgnoreCase));

                        return new EntityChecklistCandidateSummary
                        {
                            Id = document.Id,
                            EntityType = document.EntityType,
                            Name = document.Name,
                            Key = document.Key,
                            Description = document.Description,
                            CompletedTargetStepKeys = completedTargetStepKeys,
                            CompletedTargetStepCount = completedTargetStepKeys.Count,
                            TargetStepCount = normalizedChecklistStepKeys.Count,
                            TargetChecklistStatus = targetChecklistStatus
                        };
                    })
                    .ToList();

                _logger.Trace(successMessage);

                return InvokeResult<List<EntityChecklistCandidateSummary>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromException(this.Tag(), ex);
            }
        }

        private async Task<int> ExecuteScalarIntAsync(QueryDefinition qd, CancellationToken ct)
        {
            using var iterator = _container.GetItemQueryIterator<int>(qd);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                return response.FirstOrDefault();
            }

            return 0;
        }

        private static string BuildCandidateSummarySql(IEnumerable<string> predicates, int maxItems)
        {
            var sql = new System.Text.StringBuilder();

            sql.AppendLine($"SELECT TOP {maxItems}");
            sql.AppendLine("    c.id AS Id,");
            sql.AppendLine("    c.EntityType AS EntityType,");
            sql.AppendLine("    c.Name AS Name,");
            sql.AppendLine("    c.Key AS Key,");
            sql.AppendLine("    c.Description AS Description,");
            sql.AppendLine("    c.ChecklistStatus AS ChecklistStatus");
            sql.AppendLine("FROM c");
            sql.AppendLine($"WHERE {String.Join(Environment.NewLine + "AND ", predicates)}");
            sql.AppendLine("ORDER BY c.Name");

            return sql.ToString();
        }

        private static string BuildCompletedChecklistStepPredicate(string stepParameterName)
        {
            return $@"EXISTS (
    SELECT VALUE status
    FROM status IN c.ChecklistStatus
    WHERE status.StepKey = {stepParameterName}
    AND status.Status.Key = @completedStatus
)";
        }

        private static string BuildIncompleteChecklistStepPredicate(string stepParameterName)
        {
            return $@"NOT {BuildCompletedChecklistStepPredicate(stepParameterName)}";
        }

        private static InvokeResult ValidateChecklistStepKeys(IEnumerable<string> stepKeys)
        {
            foreach (var stepKey in stepKeys)
            {
                if (!IsSafeChecklistStepKey(stepKey))
                {
                    return InvokeResult.FromError($"Checklist step key '{stepKey}' is not safe for a Cosmos query.");
                }
            }

            return InvokeResult.Success;
        }

        private static List<string> NormalizeChecklistStepKeys(IEnumerable<string> stepKeys)
        {
            return stepKeys.Where(stepKey => !String.IsNullOrWhiteSpace(stepKey)).Select(stepKey => stepKey.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool IsSafeChecklistStepKey(string stepKey)
        {
            if (String.IsNullOrWhiteSpace(stepKey))
            {
                return false;
            }

            if (stepKey.Length > 128)
            {
                return false;
            }

            foreach (var ch in stepKey)
            {
                if (!(Char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.'))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<InvokeResult<List<EntityChecklistBlockedCandidateSummary>>> GetEntitiesBlockedByChecklistPrerequisitesAsync(
    string entityType,
    string orgId,
    IEnumerable<string> requiredCompletedStepKeys,
    int maxItems,
    CancellationToken ct)
        {
            const string tag = "[EntityUtilsRepository__GetEntitiesBlockedByChecklistPrerequisitesAsync]";

            try
            {
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    throw new ArgumentException("entityType is required.", nameof(entityType));
                }

                if (String.IsNullOrWhiteSpace(orgId))
                {
                    throw new ArgumentException("orgId is required.", nameof(orgId));
                }

                if (requiredCompletedStepKeys == null)
                {
                    throw new ArgumentNullException(nameof(requiredCompletedStepKeys));
                }

                if (maxItems <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be greater than zero.");
                }

                var requiredStepKeys = NormalizeChecklistStepKeys(requiredCompletedStepKeys);

                if (!requiredStepKeys.Any())
                {
                    return InvokeResult<List<EntityChecklistBlockedCandidateSummary>>.FromError(
                        "At least one required completed checklist step key is required.");
                }

                var validation = ValidateChecklistStepKeys(requiredStepKeys);

                if (!validation.Successful)
                {
                    return InvokeResult<List<EntityChecklistBlockedCandidateSummary>>.FromInvokeResult(validation);
                }

                var take = Math.Min(maxItems, 5000);
                var missingPrerequisitePredicates = new List<string>();

                for (var idx = 0; idx < requiredStepKeys.Count; idx++)
                {
                    missingPrerequisitePredicates.Add(
                        BuildIncompleteChecklistStepPredicate($"@requiredStepKey{idx}"));
                }

                var sql = BuildBlockedCandidateSql(missingPrerequisitePredicates, take);

                var qd = new QueryDefinition(sql)
                    .WithParameter("@entityType", entityType.Trim())
                    .WithParameter("@orgId", orgId.Trim())
                    .WithParameter("@completedStatus", EntityChecklistStatus.Completed);

                for (var idx = 0; idx < requiredStepKeys.Count; idx++)
                {
                    qd = qd.WithParameter($"@requiredStepKey{idx}", requiredStepKeys[idx]);
                }

                var documents = new List<EntityChecklistBlockedCandidateDocument>();
                var requestOptions = new QueryRequestOptions
                {
                    MaxItemCount = Math.Min(take, 100)
                };

                using var iterator = _container.GetItemQueryIterator<EntityChecklistBlockedCandidateDocument>(
                    qd,
                    requestOptions: requestOptions);

                while (iterator.HasMoreResults && documents.Count < take)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);

                    foreach (var item in page.Resource)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        documents.Add(item);

                        if (documents.Count >= take)
                        {
                            break;
                        }
                    }
                }


                _logger.Trace(
                    $"{this.Tag()} - Found {documents.Count} blocked checklist candidates from the query {entityType} " +
                    $"with prerequisites [{String.Join(", ", requiredStepKeys)}].");

                foreach (var document in documents)
                {
                    _logger.Trace(
                        $"{this.Tag()} - Blocked candidate '{document.Name ?? document.Id}' " +
                        $"has {document.ChecklistStatus?.Count ?? 0} checklist status entries.");

                    foreach (var status in document.ChecklistStatus)
                    {
                        _logger.Trace(
                            $"{this.Tag()} - Checklist status: StepKey='{status.StepKey}', " +
                            $"Status='{status.Status?.Key}'.");
                    }
                }


                var results = documents
                    .Select(document =>
                    {
                        var completedStepKeys = (document.ChecklistStatus)
                            .Where(status =>
                                status != null &&
                                !String.IsNullOrWhiteSpace(status.StepKey) &&
                                status.Status != null &&
                                String.Equals(
                                    status.Status.Key,
                                    EntityChecklistStatus.Completed,
                                    StringComparison.OrdinalIgnoreCase))
                            .Select(status => status.StepKey)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var missingStepKeys = requiredStepKeys
                            .Where(stepKey => !completedStepKeys.Contains(stepKey))
                            .ToList();

                        _logger.Trace(
                            $"{this.Tag()} - Candidate '{document.Name ?? document.Id}' completed " +
                            $"[{String.Join(", ", completedStepKeys)}] and is missing " +
                            $"[{String.Join(", ", missingStepKeys)}].");

                        return new EntityChecklistBlockedCandidateSummary
                        {
                            Id = document.Id,
                            EntityType = document.EntityType,
                            Name = document.Name,
                            Key = document.Key,
                            Description = document.Description,
                            MissingRequiredStepKeys = missingStepKeys
                        };
                    })
                    .Where(candidate => candidate.MissingRequiredStepKeys.Any())
                    .ToList();

                _logger.Trace(
                    $"{this.Tag()} - Found {results.Count} blocked checklist candidates for {entityType} " +
                    $"with prerequisites [{String.Join(", ", requiredStepKeys)}].");

                return InvokeResult<List<EntityChecklistBlockedCandidateSummary>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(tag, ex);
                return InvokeResult<List<EntityChecklistBlockedCandidateSummary>>.FromException(tag, ex);
            }
        }

        public async Task<InvokeResult<List<JObject>>> GetEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct)
        {
            const string tag = "[EntityUtilsRepository__GetEntitiesByTypeAsync]";

            try
            {
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    throw new ArgumentException("entityType is required.", nameof(entityType));
                }

                if (String.IsNullOrWhiteSpace(orgId))
                {
                    throw new ArgumentException("orgId is required.", nameof(orgId));
                }

                const string sql =
        @"SELECT *
FROM c
WHERE c.EntityType = @entityType
AND c.OwnerOrganization.Id = @orgId
ORDER BY c.Name ASC";

                var query = new QueryDefinition(sql)
                    .WithParameter("@entityType", entityType.Trim())
                    .WithParameter("@orgId", orgId.Trim());

                var results = new List<JObject>();
                var requestOptions = new QueryRequestOptions
                {
                    MaxItemCount = 100
                };

                using var iterator = _container.GetItemQueryIterator<JObject>(query, requestOptions: requestOptions);

                while (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);

                    foreach (var document in page.Resource)
                    {
                        if (document == null)
                        {
                            continue;
                        }

                        results.Add(document);
                    }
                }

                _logger.Trace($"{this.Tag()} - Found {results.Count} entities of type '{entityType}' for organization '{orgId}'.");

                return InvokeResult<List<JObject>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(tag, ex);
                return InvokeResult<List<JObject>>.FromException(tag, ex);
            }
        }

        private static string BuildBlockedCandidateSql(IEnumerable<string> missingPrerequisitePredicates, int maxItems)
        {
            var predicates = (missingPrerequisitePredicates ?? Enumerable.Empty<string>())
                .Where(predicate => !String.IsNullOrWhiteSpace(predicate))
                .ToList();

            if (!predicates.Any())
            {
                throw new ArgumentException(
                    "At least one missing prerequisite predicate is required.",
                    nameof(missingPrerequisitePredicates));
            }

            var sql = new System.Text.StringBuilder();

            sql.AppendLine($"SELECT TOP {maxItems}");
            sql.AppendLine("    c.id AS Id,");
            sql.AppendLine("    c.EntityType AS EntityType,");
            sql.AppendLine("    c.Name AS Name,");
            sql.AppendLine("    c.Key AS Key,");
            sql.AppendLine("    c.Description AS Description,");
            sql.AppendLine("    c.ChecklistStatus AS ChecklistStatus");
            sql.AppendLine("FROM c");
            sql.AppendLine("WHERE c.EntityType = @entityType");
            sql.AppendLine("AND c.OwnerOrganization.Id = @orgId");
            sql.AppendLine("AND (");
            sql.AppendLine($"    {String.Join(Environment.NewLine + "    OR ", predicates)}");
            sql.AppendLine(")");
            sql.AppendLine("ORDER BY c.Name");

            return sql.ToString();
        }
        private class EntityChecklistBlockedCandidateDocument
        {
            public string Id { get; set; }

            public string EntityType { get; set; }

            public string Name { get; set; }

            public string Key { get; set; }

            public string Description { get; set; }

            public List<EntityChecklistStatus> ChecklistStatus { get; set; } = new List<EntityChecklistStatus>();
        }

        private class EntityChecklistCandidateDocument
        {
            public string Id { get; set; }

            public string EntityType { get; set; }

            public string Name { get; set; }

            public string Key { get; set; }

            public string Description { get; set; }

            public List<EntityChecklistStatus> ChecklistStatus { get; set; } = new List<EntityChecklistStatus>();
        }
    }
}
