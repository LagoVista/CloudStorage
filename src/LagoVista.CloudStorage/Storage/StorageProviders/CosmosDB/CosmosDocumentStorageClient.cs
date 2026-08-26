using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Models.Storage;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    /// <summary>
    /// Cosmos-specific document persistence. Common repository concerns such as validation,
    /// caching, dependency processing, audit preparation, and cache invalidation stay above
    /// this boundary so they execute identically regardless of the selected provider.
    /// </summary>
    public sealed partial class CosmosDocumentStorageClient : ICosmosDocumentStorageClient
    {
        private readonly ICosmosConnectionSettings _settings;
        private readonly ICosmosClientProvider _cosmosClientProvider;

        public CosmosDocumentStorageClient(ICosmosConnectionSettings settings, ICosmosClientProvider cosmosClientProvider)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
        }

        public async Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var response = await GetContainer<TEntity>().CreateItemAsync(item).ConfigureAwait(false);
            return new OperationResponse<TEntity>(response);
        }

        public async Task<OperationResponse<TEntity>> UpsertDocumentAsync<TEntity>(TEntity item, string eTag = null)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var options = String.IsNullOrWhiteSpace(eTag)
                ? null
                : new ItemRequestOptions { IfMatchEtag = eTag };

            try
            {
                var response = await GetContainer<TEntity>().UpsertItemAsync(item, requestOptions: options).ConfigureAwait(false);
                return new OperationResponse<TEntity>(response);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed || ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new ContentModifiedException { EntityType = typeof(TEntity).Name, Id = item.Id };
            }
        }

        public async Task DeleteDocumentAsync(string entityType, string id, string partitionKey = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));

            try
            {
                await GetRawDocumentContainer().DeleteItemAsync<JObject>(id, String.IsNullOrWhiteSpace(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new RecordNotFoundException(entityType, id);
            }
        }

        public async Task<InvokeResult> PatchDocumentAsync(string entityType, PatchRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrWhiteSpace(request.Id)) throw new ArgumentException("Patch request id is required.", nameof(request));
            if (request.Steps == null || request.Steps.Count == 0) throw new ArgumentException("Patch request must contain at least one step.", nameof(request));

            var operations = request.Steps.Select(CreatePatchOperation).ToList();
            var options = new PatchItemRequestOptions();

            if (!String.IsNullOrWhiteSpace(request.ETag))
                options.IfMatchEtag = request.ETag;

            try
            {
                await GetRawDocumentContainer().PatchItemAsync<JObject>(request.Id, String.IsNullOrWhiteSpace(request.PartitionKey) ? PartitionKey.None : new PartitionKey(request.PartitionKey), operations, options, cancellationToken).ConfigureAwait(false);

                return InvokeResult.Success;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new RecordNotFoundException(entityType, request.Id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed || ex.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ContentModifiedException { EntityType = entityType, Id = request.Id };
            }
        }

        public async Task<DocumentStorageWriteResult> UpsertRawDocumentAsync(string entityType, string id, string json, string expectedETag = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));
            if (String.IsNullOrWhiteSpace(json)) throw new ArgumentException("Document JSON is required.", nameof(json));

            var options = new ItemRequestOptions();

            if (!String.IsNullOrWhiteSpace(expectedETag))
                options.IfMatchEtag = expectedETag;

            var bytes = Encoding.UTF8.GetBytes(json);
            using var stream = new MemoryStream(bytes);

            using var response = await GetRawDocumentContainer().UpsertItemStreamAsync(stream, PartitionKey.None, options, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.PreconditionFailed || response.StatusCode == HttpStatusCode.Conflict)
                throw new ContentModifiedException { EntityType = entityType, Id = id };

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content == null ? null : await new StreamReader(response.Content).ReadToEndAsync().ConfigureAwait(false);
                throw new InvalidOperationException($"Raw document upsert failed ({(int)response.StatusCode} {response.StatusCode}). {body}");
            }

            return new DocumentStorageWriteResult
            {
                ETag = response.Headers?.ETag,
                StatusCode = (int)response.StatusCode,
                RequestCharge = response.Headers?.RequestCharge
            };
        }


        public async Task<DocumentPage<TProjection>> GetDocumentPageAsync<TProjection>(string entityType = null, string continuationToken = null, int pageSize = 100, CancellationToken cancellationToken = default) where TProjection : class
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var sql = "SELECT * FROM c";

            QueryDefinition query;
            if (String.IsNullOrWhiteSpace(entityType))
                query = new QueryDefinition(sql);
            else
                query = new QueryDefinition(sql + " WHERE c.EntityType = @entityType").WithParameter("@entityType", entityType);

            var options = new QueryRequestOptions { MaxItemCount = pageSize };
            using var iterator = GetRawCollection().GetItemQueryIterator<TProjection>(query, continuationToken: continuationToken, requestOptions: options);

            if (!iterator.HasMoreResults)
            {
                return new DocumentPage<TProjection>
                {
                    Items = Array.Empty<TProjection>(),
                    ContinuationToken = null
                };
            }

            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            return new DocumentPage<TProjection>
            {
                Items = response.Resource.ToList(),
                ContinuationToken = response.ContinuationToken
            };
        }

        private Container GetRawCollection()
        {
            return _cosmosClientProvider.GetClient(_settings.Endpoint, _settings.AccessKey).GetContainer(_settings.DatabaseName, $"{_settings.DatabaseName}_Collections");
        }

        private Container GetRawDocumentContainer()
        {
            return _cosmosClientProvider.GetClient(_settings.Endpoint, _settings.AccessKey).GetContainer(_settings.DatabaseName, $"{_settings.DatabaseName}_Collections");
        }

        public Task<TEntity> GetDocumentAsync<TEntity>(string id, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetDocumentAsync<TEntity>(id, null, throwOnNotFound);

        public async Task<TEntity> GetDocumentAsync<TEntity>(string id, string partitionKey, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            try
            {
                TEntity entity;

                if (String.IsNullOrWhiteSpace(partitionKey))
                {
                    var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.id = @id AND c.EntityType = @entityType")
                        .WithParameter("@id", id)
                        .WithParameter("@entityType", typeof(TEntity).Name);

                    using var iterator = GetContainer<TEntity>().GetItemQueryIterator<TEntity>(query, requestOptions: new QueryRequestOptions { MaxItemCount = 1 });
                    entity = null;
                    if (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync().ConfigureAwait(false);
                        entity = response.Resource.FirstOrDefault();
                    }
                }
                else
                {
                    var response = await GetContainer<TEntity>().ReadItemAsync<TEntity>(id, new PartitionKey(partitionKey)).ConfigureAwait(false);
                    entity = response.Resource;
                }

                if (entity == null || !String.Equals(entity.EntityType, typeof(TEntity).Name, StringComparison.Ordinal))
                {
                    if (throwOnNotFound) throw new RecordNotFoundException(typeof(TEntity).Name, id);
                    return null;
                }

                return entity;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (throwOnNotFound) throw new RecordNotFoundException(typeof(TEntity).Name, id);
                return null;
            }
        }

        public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            DeleteDocumentAsync<TEntity>(id, null);

        public async Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id, string partitionKey)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            try
            {
                var response = await GetContainer<TEntity>().DeleteItemAsync<TEntity>(
                    id,
                    String.IsNullOrWhiteSpace(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey)).ConfigureAwait(false);
                return new OperationResponse<TEntity>(response);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new RecordNotFoundException(typeof(TEntity).Name, id);
            }
        }

        public async Task<OperationResponse<TEntity>> PatchDocumentAsync<TEntity>(PatchRequest request)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrWhiteSpace(request.Id)) throw new ArgumentException("Patch request id is required.", nameof(request));
            if (request.Steps == null || request.Steps.Count == 0) throw new ArgumentException("Patch request must contain at least one step.", nameof(request));

            var operations = request.Steps.Select(CreatePatchOperation).ToList();
            var options = new PatchItemRequestOptions();
            if (!String.IsNullOrWhiteSpace(request.ETag)) options.IfMatchEtag = request.ETag;

            try
            {
                var response = await GetContainer<TEntity>().PatchItemAsync<TEntity>(
                    request.Id,
                    String.IsNullOrWhiteSpace(request.PartitionKey) ? PartitionKey.None : new PartitionKey(request.PartitionKey),
                    operations,
                    options).ConfigureAwait(false);
                return new OperationResponse<TEntity>(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new RecordNotFoundException(typeof(TEntity).Name, request.Id);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                throw new ContentModifiedException { EntityType = typeof(TEntity).Name, Id = request.Id };
            }
        }

        public async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var items = new List<TEntity>();
            var linqQuery = GetContainer<TEntity>().GetItemLinqQueryable<TEntity>()
                .Where(query)
                .Where(item => item.EntityType == typeof(TEntity).Name);

            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                    items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return items;
        }

        public async Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var items = new List<TEntity>();
            var linqQuery = GetContainer<TEntity>().GetItemLinqQueryable<TEntity>()
                .Where(query)
                .Where(item => item.EntityType == typeof(TEntity).Name &&
                               (listRequest.ShowDeleted || item.IsDeleted.IsNull() || !item.IsDeleted.HasValue || !item.IsDeleted.Value) &&
                               (listRequest.ShowDrafts || !item.IsDraft.IsDefined() || item.IsDraft == false))
                .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                .Take(listRequest.PageSize);

            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                    items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return ListResponse<TEntity>.Create(listRequest, items);
        }

        public Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            QueryAsync(query, sort, listRequest, false);

        public async Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest, bool descending)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (sort == null) throw new ArgumentNullException(nameof(sort));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var baseQuery = GetContainer<TEntity>().GetItemLinqQueryable<TEntity>()
                .Where(query)
                .Where(item => item.EntityType == typeof(TEntity).Name &&
                               (listRequest.ShowDeleted || item.IsDeleted.IsNull() || !item.IsDeleted.HasValue || !item.IsDeleted.Value) &&
                               (listRequest.ShowDrafts || !item.IsDraft.IsDefined() || item.IsDraft == false));

            var orderedQuery = descending ? baseQuery.OrderByDescending(sort) : baseQuery.OrderBy(sort);
            var linqQuery = orderedQuery
                .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                .Take(listRequest.PageSize);

            var items = new List<TEntity>();
            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                    items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return ListResponse<TEntity>.Create(listRequest, items);
        }

        public async Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(string entityType, DocumentQueryRequest request, CancellationToken cancellationToken = default)
            where TResult : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var iterator = GetContainer<TResult>().GetItemQueryIterator<TResult>(CreateKnownQuery(request));
            var items = new List<TResult>();
            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false));
            return items;
        }

        private Container GetContainer<TEntity>()
        {
            var containerName = typeof(TEntity).GetCustomAttributes(typeof(CollectionNameAttribute), true)
                .OfType<CollectionNameAttribute>()
                .FirstOrDefault()
                ?.CollectionName
                ?? $"{_settings.DatabaseName}_Collections";

            return _cosmosClientProvider.GetClient(_settings.Endpoint, _settings.AccessKey)
                .GetContainer(_settings.DatabaseName, containerName);
        }

        private static PatchOperation CreatePatchOperation(PatchStep step)
        {
            if (step == null) throw new ArgumentException("Patch request contains a null step.");
            var path = !String.IsNullOrWhiteSpace(step.CosmosPath)
                ? step.CosmosPath
                : ToCosmosPath(step.LogicalPath);

            switch (step.Op)
            {
                case PatchOp.Set:
                    return PatchOperation.Set(path, step.Value?.ToObject<object>());
                case PatchOp.Remove:
                    return PatchOperation.Remove(path);
                case PatchOp.Add:
                    return PatchOperation.Add(path, step.Value?.ToObject<object>());
                default:
                    throw new NotSupportedException($"Patch operation '{step.Op}' is not supported by the Cosmos document client.");
            }
        }

        private static string ToCosmosPath(string logicalPath)
        {
            if (String.IsNullOrWhiteSpace(logicalPath)) throw new ArgumentException("Patch step path is required.");
            return "/" + logicalPath.Trim().TrimStart('/').Replace('.', '/');
        }

        private static bool IsSafeDocumentPropertyName(string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName) || fieldName.Length > 128)
                return false;

            foreach (var ch in fieldName)
            {
                if (!(Char.IsLetterOrDigit(ch) || ch == '_'))
                    return false;
            }

            return true;
        }


        private static QueryDefinition CreateKnownQuery(DocumentQueryRequest request)
        {
            switch (request.QueryType)
            {
                case DocumentQueryType.EntityUtilsDocumentsByStatusIds:
                    {
                        var maxItems = Math.Min(request.GetRequired<int>("maxItems"), 5000);

                        return new QueryDefinition($@"
SELECT TOP {maxItems} *
FROM c
WHERE c.EntityType = @entityType
AND c.OwnerOrganization.Id = @orgId
AND (
    NOT IS_DEFINED(c.Status)
    OR IS_NULL(c.Status)
    OR NOT IS_DEFINED(c.Status.Id)
    OR IS_NULL(c.Status.Id)
    OR ARRAY_CONTAINS(@statusIds, c.Status.Id)
)
ORDER BY c.Name ASC")
                            .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                            .WithParameter("@orgId", request.GetRequired<string>("orgId"))
                            .WithParameter("@statusIds", request.GetRequired<List<string>>("statusIds"));
                    }

                case DocumentQueryType.EntityUtilsDocumentsByFieldValue:
                    {
                        var fieldName = request.GetRequired<string>("fieldName");

                        if (!IsSafeDocumentPropertyName(fieldName))
                            throw new ArgumentException($"Field name '{fieldName}' is not safe for a document query.");

                        return new QueryDefinition($@"
SELECT c.id AS Id
FROM c
WHERE c.EntityType = @entityType
AND c.OwnerOrganization.Id = @orgId
AND c[""{fieldName}""] = @value")
                            .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                            .WithParameter("@orgId", request.GetRequired<string>("orgId"))
                            .WithParameter("@value", request.GetRequired<string>("value"));
                    }

                case DocumentQueryType.EntityUtilsDocumentsWithEmptyField:
                    {
                        var maxItems = Math.Min(request.GetRequired<int>("maxItems"), 5000);
                        var fieldName = request.GetRequired<string>("fieldName");

                        if (!IsSafeDocumentPropertyName(fieldName))
                            throw new ArgumentException($"Field name '{fieldName}' is not safe for a document query.");

                        return new QueryDefinition($@"
SELECT TOP {maxItems} *
FROM c
WHERE c.EntityType = @entityType
AND c.OwnerOrganization.Id = @orgId
AND (
    NOT IS_DEFINED(c[""{fieldName}""])
    OR IS_NULL(c[""{fieldName}""])
    OR c[""{fieldName}""] = ''
)")
                            .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                            .WithParameter("@orgId", request.GetRequired<string>("orgId"));
                    }
                case DocumentQueryType.EntityUtilsCompletedChecklistCandidates:
                    return CreateCompletedChecklistQuery(request, false);

                case DocumentQueryType.EntityUtilsCompletedChecklistCount:
                    return CreateCompletedChecklistQuery(request, true);
                case DocumentQueryType.CustomerIndustryNicheSalesStageCounts:
                    return new QueryDefinition("SELECT c.Industry, c.IndustryNiche, c.SalesStage, COUNT(c.id) AS CountLeads FROM c WHERE c.EntityType = 'CustomerEntity' AND c.OwnerOrganization.Id = @orgId GROUP BY c.Industry, c.IndustryNiche, c.SalesStage")
                        .WithParameter("@orgId", request.GetRequired<string>("orgId"));
                case DocumentQueryType.EntityUtilsDocumentsByType:
                    return new QueryDefinition("SELECT * FROM c WHERE c.EntityType = @entityType AND c.OwnerOrganization.Id = @orgId ORDER BY c.Name ASC")
                        .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                        .WithParameter("@orgId", request.GetRequired<string>("orgId"));
                case DocumentQueryType.EntityUtilsDocumentById:
                    return new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.EntityType = @entityType AND c.id = @entityId AND c.OwnerOrganization.Id = @orgId")
                        .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                        .WithParameter("@entityId", request.GetRequired<string>("entityId"))
                        .WithParameter("@orgId", request.GetRequired<string>("orgId"));
                case DocumentQueryType.EntityUtilsCountByType:
                    return new QueryDefinition("SELECT COUNT(1) AS Count FROM c WHERE c.EntityType = @entityType AND c.OwnerOrganization.Id = @orgId")
                        .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                        .WithParameter("@orgId", request.GetRequired<string>("orgId"));
                case DocumentQueryType.EntityPreparationCandidateById:
                    return new QueryDefinition($"SELECT TOP 1 {EntityPreparationProjection} FROM c WHERE c.EntityType = @entityType AND c.id = @entityId AND c.OwnerOrganization.Id = @orgId")
                        .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                        .WithParameter("@entityId", request.GetRequired<string>("entityId"))
                        .WithParameter("@orgId", request.GetRequired<string>("orgId"));
                case DocumentQueryType.EntityPreparationCandidatesByType:
                case DocumentQueryType.IncompleteEntityPreparationCandidatesByType:
                    return CreatePreparationQuery(request);
                case DocumentQueryType.EntityListItems:
                case DocumentQueryType.EntityListHeaders:
                case DocumentQueryType.EntityListCategories:
                    return CreateEntityListQuery(request);
                default:
                    throw new NotSupportedException($"Registered document query '{request.QueryType}' is not implemented by the Cosmos provider.");
            }
        }

        private static QueryDefinition CreateCompletedChecklistQuery(DocumentQueryRequest request, bool count)
        {
            var stepKeys = request.GetRequired<List<string>>("stepKeys");

            var predicates = new List<string>
    {
        "c.EntityType = @entityType",
        "c.OwnerOrganization.Id = @orgId"
    };

            for (var idx = 0; idx < stepKeys.Count; idx++)
            {
                predicates.Add($@"EXISTS (
    SELECT VALUE status
    FROM status IN c.ChecklistStatus
    WHERE status.StepKey = @stepKey{idx}
    AND IS_DEFINED(status.LastRun)
    AND NOT IS_NULL(status.LastRun)
)");
            }

            string sql;

            if (count)
            {
                sql = $"SELECT COUNT(1) AS Count FROM c WHERE {String.Join(" AND ", predicates)}";
            }
            else
            {
                var maxItems = Math.Min(request.GetRequired<int>("maxItems"), 5000);

                sql = $@"SELECT TOP {maxItems}
    c.id AS Id,
    c.EntityType AS EntityType,
    c.Name AS Name,
    c.Key AS Key,
    c.Description AS Description,
    c.ChecklistStatus AS ChecklistStatus
FROM c
WHERE {String.Join(Environment.NewLine + "AND ", predicates)}
ORDER BY c.Name";
            }

            var query = new QueryDefinition(sql)
                .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                .WithParameter("@orgId", request.GetRequired<string>("orgId"));

            for (var idx = 0; idx < stepKeys.Count; idx++)
                query = query.WithParameter($"@stepKey{idx}", stepKeys[idx]);

            return query;
        }

        private static QueryDefinition CreatePreparationQuery(DocumentQueryRequest request)
        {
            var incomplete = request.QueryType == DocumentQueryType.IncompleteEntityPreparationCandidatesByType;
            var top = incomplete ? $"TOP {Math.Min(request.GetRequired<int>("maxItems"), 5000)} " : String.Empty;
            var incompleteClause = incomplete ? " AND (NOT IS_DEFINED(c.MasterStatus) OR IS_NULL(c.MasterStatus) OR NOT IS_DEFINED(c.MasterStatus.IsProductionReady) OR IS_NULL(c.MasterStatus.IsProductionReady) OR c.MasterStatus.IsProductionReady != true)" : String.Empty;
            return new QueryDefinition($"SELECT {top}{EntityPreparationProjection} FROM c WHERE c.EntityType = @entityType AND c.OwnerOrganization.Id = @orgId{incompleteClause} ORDER BY c.Name ASC")
                .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                .WithParameter("@orgId", request.GetRequired<string>("orgId"));
        }

        private static QueryDefinition CreateEntityListQuery(DocumentQueryRequest request)
        {
            var type = request.QueryType;
            var sql = type == DocumentQueryType.EntityListItems
                ? @"SELECT VALUE {""id"": c.id, ""Icon"": c.Icon, ""Name"": c.Name, ""Key"": c.Key, ""IsPublic"": c.IsPublic, ""IsDraft"": c.IsDraft, ""IsDeleted"": c.IsDeleted, ""Category"": c.Category.Text, ""Stars"": c.Stars, ""RatingsCount"": c.RatingsCount, ""Labels"": c.Labels, ""Status"": c.Status} FROM c"
                : type == DocumentQueryType.EntityListHeaders
                    ? @"SELECT VALUE {""Id"": c.id, ""Key"": c.Key, ""Text"": c.Name} FROM c"
                    : @"SELECT DISTINCT VALUE {""Id"": c.Category.Id, ""Key"": c.Category.Key, ""Text"": c.Category.Text} FROM c";
            sql += " WHERE c.EntityType = @entityType AND (c.IsPublic = true OR c.OwnerOrganization.Id = @orgId)";
            if (type == DocumentQueryType.EntityListCategories) sql += " AND IS_DEFINED(c.Category) AND IS_DEFINED(c.Category.Key)";
            if (!request.GetRequired<bool>("showDeleted")) sql += " AND (NOT IS_DEFINED(c.IsDeleted) OR c.IsDeleted = false)";
            if (!request.GetRequired<bool>("showDrafts")) sql += " AND (NOT IS_DEFINED(c.IsDraft) OR c.IsDraft = false)";
            if (type != DocumentQueryType.EntityListCategories)
            {
                if (!String.IsNullOrWhiteSpace(request.GetRequired<string>("categoryKey"))) sql += " AND c.Category.Key = @categoryKey";
                if (!String.IsNullOrWhiteSpace(request.GetRequired<string>("statusKey"))) sql += " AND c.Status.Key = @statusKey";
                if (!String.IsNullOrWhiteSpace(request.GetRequired<string>("labelKey"))) sql += " AND ARRAY_CONTAINS(c.Labels, {\"Key\": @labelKey}, true)";
                if (!String.IsNullOrWhiteSpace(request.GetRequired<string>("searchText"))) sql += " AND CONTAINS(c.Name, @searchText, true)";
            }
            if (type == DocumentQueryType.EntityListCategories) sql += " ORDER BY c.Category.Text";
            else
            {
                var orderBy = (OrderByTypes)request.GetRequired<int>("orderBy");
                var field = orderBy == OrderByTypes.Rating ? "c.Stars" : orderBy == OrderByTypes.CreationDate ? "c.CreationDate" : orderBy == OrderByTypes.LastUpdateDate ? "c.LastUpdatedDate" : "c.Name";
                sql += $" ORDER BY {field}{(request.GetRequired<bool>("descending") ? " DESC" : String.Empty)}";
                var pageIndex = Math.Max(1, request.GetRequired<int>("pageIndex"));
                var pageSize = Math.Max(1, request.GetRequired<int>("pageSize"));
                sql += $" OFFSET {(pageIndex - 1) * pageSize} LIMIT {pageSize}";
            }
            var query = new QueryDefinition(sql)
                .WithParameter("@entityType", request.GetRequired<string>("entityType"))
                .WithParameter("@orgId", request.GetRequired<string>("orgId"));
            if (type != DocumentQueryType.EntityListCategories)
            {
                var categoryKey = request.GetRequired<string>("categoryKey");
                var statusKey = request.GetRequired<string>("statusKey");
                var labelKey = request.GetRequired<string>("labelKey");
                var searchText = request.GetRequired<string>("searchText");
                if (!String.IsNullOrWhiteSpace(categoryKey)) query.WithParameter("@categoryKey", categoryKey);
                if (!String.IsNullOrWhiteSpace(statusKey)) query.WithParameter("@statusKey", statusKey);
                if (!String.IsNullOrWhiteSpace(labelKey)) query.WithParameter("@labelKey", labelKey);
                if (!String.IsNullOrWhiteSpace(searchText)) query.WithParameter("@searchText", searchText);
            }
            return query;
        }

        private const string EntityPreparationProjection = "c.id AS Id, c.EntityType AS EntityType, c.Name AS Name, c.Key AS Key, c.Description AS Description, c.Icon AS Icon, c.Category AS Category, c.IsDraft AS IsDraft, c.IsDeprecated AS IsDeprecated, c.MasterStatus AS MasterStatus, c.ReadinessStatus AS ReadinessStatus, c.CreationDate AS CreationDate, c.LastUpdatedDate AS LastUpdatedDate, c.Revision AS Revision, c.ChecklistStatus AS ChecklistStatus, c.ReadinessChecks AS ReadinessChecks";
    }
}
