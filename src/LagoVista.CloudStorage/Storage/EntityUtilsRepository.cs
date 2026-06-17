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

        public Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(
    string entityType,
    string orgId,
    IEnumerable<EntityChecklistStep> checklistSteps,
    int maxItems,
    CancellationToken ct)
        {
            if (checklistSteps == null)
            {
                throw new ArgumentNullException(nameof(checklistSteps));
            }

            var checklistStepKeys = checklistSteps
                .Where(step => step != null && !String.IsNullOrWhiteSpace(step.Key))
                .Select(step => step.Key)
                .ToList();

            return GetEntitiesWithCompletedChecklistStepsAsync(
                entityType,
                orgId,
                checklistStepKeys,
                maxItems,
                ct);
        }

        public async Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(
            string entityType,
            string orgId,
            IEnumerable<string> checklistStepKeys,
            int maxItems,
            CancellationToken ct)
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

            var requiredStepKeys = checklistStepKeys
                .Where(stepKey => !String.IsNullOrWhiteSpace(stepKey))
                .Select(stepKey => stepKey.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!requiredStepKeys.Any())
            {
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromError(
                    "At least one checklist step key is required.");
            }

            foreach (var stepKey in requiredStepKeys)
            {
                if (!IsSafeChecklistStepKey(stepKey))
                {
                    return InvokeResult<List<EntityChecklistCandidateSummary>>.FromError(
                        $"Checklist step key '{stepKey}' is not safe for a Cosmos query.");
                }
            }

            var sql = BuildCompletedChecklistStepsSql(requiredStepKeys, maxItems);

            var qd = new QueryDefinition(sql)
                .WithParameter("@entityType", entityType.Trim())
                .WithParameter("@orgId", orgId.Trim())
                .WithParameter("@completedStatus", EntityChecklistStatus.Completed);

            for (var idx = 0; idx < requiredStepKeys.Count; idx++)
            {
                qd = qd.WithParameter($"@stepKey{idx}", requiredStepKeys[idx]);
            }

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = Math.Min(maxItems, 100)
            };

            var results = new List<EntityChecklistCandidateSummary>();

            try
            {
                using (var iterator = _container.GetItemQueryIterator<EntityChecklistCandidateSummary>(
                    qd,
                    requestOptions: requestOptions))
                {
                    while (iterator.HasMoreResults && results.Count < maxItems)
                    {
                        var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);

                        foreach (var item in page.Resource)
                        {
                            if (item == null)
                            {
                                continue;
                            }

                            results.Add(item);

                            if (results.Count >= maxItems)
                            {
                                break;
                            }
                        }
                    }
                }

                _logger.Trace(
                    $"{this.Tag()} - Found {results.Count} {entityType} entities with completed checklist steps [{String.Join(", ", requiredStepKeys)}].");

                return InvokeResult<List<EntityChecklistCandidateSummary>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromException(this.Tag(), ex);
            }
        }

        public async Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, string targetIncompleteStepKey, int maxItems, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("entityType is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("orgId is required.", nameof(orgId));
            if (requiredCompletedStepKeys == null) throw new ArgumentNullException(nameof(requiredCompletedStepKeys));
            if (String.IsNullOrWhiteSpace(targetIncompleteStepKey)) throw new ArgumentException("targetIncompleteStepKey is required.", nameof(targetIncompleteStepKey));
            if (maxItems <= 0) throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be greater than zero.");

            var requiredStepKeys = requiredCompletedStepKeys.Where(stepKey => !String.IsNullOrWhiteSpace(stepKey)).Select(stepKey => stepKey.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var targetStepKey = targetIncompleteStepKey.Trim();

            if (!requiredStepKeys.Any()) return InvokeResult<List<EntityChecklistCandidateSummary>>.FromError("At least one required completed checklist step key is required.");
            if (!IsSafeChecklistStepKey(targetStepKey)) return InvokeResult<List<EntityChecklistCandidateSummary>>.FromError($"Target checklist step key '{targetStepKey}' is not safe for a Cosmos query.");

            foreach (var stepKey in requiredStepKeys)
            {
                if (!IsSafeChecklistStepKey(stepKey)) return InvokeResult<List<EntityChecklistCandidateSummary>>.FromError($"Checklist step key '{stepKey}' is not safe for a Cosmos query.");
            }

            var take = Math.Min(maxItems, 250);
            var results = new List<EntityChecklistCandidateSummary>();

            var sql = BuildReadyForChecklistStepSql(requiredStepKeys, take);

            var qd = new QueryDefinition(sql)
                .WithParameter("@entityType", entityType.Trim())
                .WithParameter("@orgId", orgId.Trim())
                .WithParameter("@completedStatus", EntityChecklistStatus.Completed)
                .WithParameter("@targetStepKey", targetStepKey);

            for (var idx = 0; idx < requiredStepKeys.Count; idx++)
            {
                qd = qd.WithParameter($"@stepKey{idx}", requiredStepKeys[idx]);
            }

            var requestOptions = new QueryRequestOptions { MaxItemCount = Math.Min(take, 100) };

            try
            {
                using var iterator = _container.GetItemQueryIterator<JObject>(qd, requestOptions: requestOptions);

                while (iterator.HasMoreResults && results.Count < take)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);

                    foreach (var doc in page.Resource)
                    {
                        if (doc == null) continue;

                        results.Add(ToChecklistCandidateSummary(doc));

                        if (results.Count >= take) break;
                    }
                }

                _logger.Trace($"{this.Tag()} - Found {results.Count} {entityType} entities ready for checklist step '{targetStepKey}' with prerequisites [{String.Join(", ", requiredStepKeys)}].");

                return InvokeResult<List<EntityChecklistCandidateSummary>>.Create(results);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                return InvokeResult<List<EntityChecklistCandidateSummary>>.FromException(this.Tag(), ex);
            }
        }

        private static EntityChecklistCandidateSummary ToChecklistCandidateSummary(JObject item)
        {
            return new EntityChecklistCandidateSummary
            {
                Id = item.Value<string>("Id") ?? item.Value<string>("id"),
                EntityType = item.Value<string>("EntityType"),
                Name = item.Value<string>("Name"),
                Key = item.Value<string>("Key"),
                Description = item.Value<string>("Description"),
            };
        }

        private static string BuildReadyForChecklistStepSql(List<string> requiredStepKeys, int maxItems)
        {
            var sql = new System.Text.StringBuilder();

            sql.AppendLine($"SELECT TOP {maxItems}");
            sql.AppendLine("    c.id AS Id,");
            sql.AppendLine("    c.EntityType AS EntityType,");
            sql.AppendLine("    c.Name AS Name,");
            sql.AppendLine("    c.Key AS Key,");
            sql.AppendLine("    c.Description AS Description");
            sql.AppendLine("FROM c");
            sql.AppendLine("WHERE c.EntityType = @entityType");
            sql.AppendLine("AND c.OwnerOrganization.Id = @orgId");

            for (var idx = 0; idx < requiredStepKeys.Count; idx++)
            {
                sql.AppendLine($@"AND EXISTS (
    SELECT VALUE status
    FROM status IN c.ChecklistStatus
    WHERE status.StepKey = @stepKey{idx}
    AND status.Status.Id = @completedStatus
)");
            }

            sql.AppendLine(@"AND NOT EXISTS (
    SELECT VALUE status
    FROM status IN c.ChecklistStatus
    WHERE status.StepKey = @targetStepKey
    AND status.Status.Id = @completedStatus
)");

            sql.AppendLine("ORDER BY c.Name");

            return sql.ToString();
        }

        private static string BuildCompletedChecklistStepsSql(
            List<string> requiredStepKeys,
            int maxItems)
        {
            var sql = new System.Text.StringBuilder();

            sql.AppendLine($"SELECT TOP {maxItems}");
            sql.AppendLine("    c.id AS Id,");
            sql.AppendLine("    c.EntityType AS EntityType,");
            sql.AppendLine("    c.Name AS Name,");
            sql.AppendLine("    c.Key AS Key,");
            sql.AppendLine("    c.Description AS Description");
            sql.AppendLine("FROM c");
            sql.AppendLine("WHERE c.EntityType = @entityType");
            sql.AppendLine("AND c.OwnerOrganization.Id = @orgId");

            for (var idx = 0; idx < requiredStepKeys.Count; idx++)
            {
                sql.AppendLine($@"AND EXISTS (
    SELECT VALUE status
    FROM status IN c.ChecklistStatus
    WHERE status.StepKey = @stepKey{idx}
    AND status.Status.Id = @completedStatus
)");
            }

            sql.AppendLine("ORDER BY c.Name");

            return sql.ToString();
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
    }
}
