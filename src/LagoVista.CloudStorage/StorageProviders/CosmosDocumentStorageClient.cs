using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class CosmosDocumentStorageClient : ICosmosDocumentStorageClient
    {
        private readonly ICosmosConnectionSettings _settings;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IDependencyManager _dependencyManager;
        private readonly ICosmosClientProvider _cosmosClientProvider;

        public CosmosDocumentStorageClient(
            ICosmosConnectionSettings settings,
            IAdminLogger logger,
            ICacheProvider cacheProvider,
            IDependencyManager dependencyManager,
            ICosmosClientProvider cosmosClientProvider)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider;
            _dependencyManager = dependencyManager;
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
        }

        public async Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            Validate(item);
            Prepare(item);
            var response = await GetContainer().CreateItemAsync(item).ConfigureAwait(false);
            if (_cacheProvider != null) await _cacheProvider.AddAsync(GetCacheKey<TEntity>(item.Id), JsonConvert.SerializeObject(item)).ConfigureAwait(false);
            return new OperationResponse<TEntity>(response);
        }

        public async Task<OperationResponse<TEntity>> UpsertDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            Validate(item);
            Prepare(item);

            if (_dependencyManager != null)
            {
                var existing = await GetDocumentAsync<TEntity>(item.Id, false).ConfigureAwait(false);
                if (existing != null && !String.Equals(existing.Name, item.Name, StringComparison.Ordinal))
                {
                    var dependencyResult = await _dependencyManager.CheckForDependenciesAsync(item).ConfigureAwait(false);
                    if (dependencyResult.IsInUse)
                    {
                        foreach (var dependent in dependencyResult.DependentObjects)
                            await _dependencyManager.RenameDependentObjectsAsync(item.LastUpdatedBy, item.Id, item.GetType().Name, dependent.Id, dependent.RecordType, item.Name).ConfigureAwait(false);
                    }
                    await _dependencyManager.RenameObjectAsync(item.LastUpdatedBy, item.Id, item.GetType().Name, item.Name).ConfigureAwait(false);
                }
            }

            var response = await GetContainer().UpsertItemAsync(item).ConfigureAwait(false);
            if (_cacheProvider != null) await _cacheProvider.AddAsync(GetCacheKey<TEntity>(item.Id), JsonConvert.SerializeObject(item)).ConfigureAwait(false);
            return new OperationResponse<TEntity>(response);
        }

        public async Task<TEntity> GetDocumentAsync<TEntity>(string id, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (_cacheProvider != null)
            {
                var cached = await _cacheProvider.GetAsync(GetCacheKey<TEntity>(id)).ConfigureAwait(false);
                if (!String.IsNullOrWhiteSpace(cached))
                {
                    var entity = JsonConvert.DeserializeObject<TEntity>(cached);
                    if (entity != null && String.Equals(entity.EntityType, typeof(TEntity).Name, StringComparison.Ordinal)) return entity;
                    await _cacheProvider.RemoveAsync(GetCacheKey<TEntity>(id)).ConfigureAwait(false);
                }
            }

            var entityFromStore = await GetDocumentAsync<TEntity>(id, null, throwOnNotFound).ConfigureAwait(false);
            if (entityFromStore != null && _cacheProvider != null)
                await _cacheProvider.AddAsync(GetCacheKey<TEntity>(id), JsonConvert.SerializeObject(entityFromStore)).ConfigureAwait(false);
            return entityFromStore;
        }

        public async Task<TEntity> GetDocumentAsync<TEntity>(string id, string partitionKey, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            try
            {
                var response = await GetContainer().ReadItemAsync<TEntity>(id, String.IsNullOrWhiteSpace(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey)).ConfigureAwait(false);
                var entity = response.Resource;
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
            var document = await GetDocumentAsync<TEntity>(id, partitionKey, true).ConfigureAwait(false);
            if (_dependencyManager != null)
            {
                var dependencies = await _dependencyManager.CheckForDependenciesAsync(document).ConfigureAwait(false);
                if (dependencies.IsInUse) throw new InUseException(dependencies);
            }
            if (_cacheProvider != null) await _cacheProvider.RemoveAsync(GetCacheKey<TEntity>(id)).ConfigureAwait(false);
            var response = await GetContainer().DeleteItemAsync<TEntity>(id, String.IsNullOrWhiteSpace(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey)).ConfigureAwait(false);
            return new OperationResponse<TEntity>(response);
        }

        public async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var items = new List<TEntity>();
            var linqQuery = GetContainer().GetItemLinqQueryable<TEntity>().Where(query).Where(item => item.EntityType == typeof(TEntity).Name);
            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults) items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }
            return items;
        }

        public async Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));
            try
            {
                var items = new List<TEntity>();
                var linqQuery = GetContainer().GetItemLinqQueryable<TEntity>()
                    .Where(query)
                    .Where(item => item.EntityType == typeof(TEntity).Name)
                    .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                    .Take(listRequest.PageSize);
                using (var iterator = linqQuery.ToFeedIterator())
                {
                    while (iterator.HasMoreResults) items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
                }
                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                _logger.AddException(nameof(CosmosDocumentStorageClient), ex);
                var response = ListResponse<TEntity>.Create(new List<TEntity>());
                response.Errors.Add(new ErrorMessage(ex.Message));
                return response;
            }
        }

        public async Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(string entityType, DocumentQueryRequest request, CancellationToken cancellationToken = default)
            where TResult : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var iterator = GetContainer().GetItemQueryIterator<TResult>(CreateKnownQuery(request));
            var items = new List<TResult>();
            while (iterator.HasMoreResults) items.AddRange(await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false));
            return items;
        }

        private Container GetContainer() => _cosmosClientProvider.GetClient(_settings.Endpoint, _settings.AccessKey).GetContainer(_settings.DatabaseName, $"{_settings.DatabaseName}_Collections");

        private string GetCacheKey<TEntity>(string id) => $"{_settings.DatabaseName}-{typeof(TEntity).Name}-{id}".ToLowerInvariant();

        private void Prepare<TEntity>(TEntity item) where TEntity : INoSQLEntity
        {
            item.DatabaseName = _settings.DatabaseName;
            item.EntityType = typeof(TEntity).Name;
        }

        private static void Validate<TEntity>(TEntity item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item is IValidateable validateable)
            {
                var result = Validator.Validate(validateable);
                if (!result.Successful) throw new ValidationException("Invalid Data.", result.Errors);
            }
        }

        private static QueryDefinition CreateKnownQuery(DocumentQueryRequest request)
        {
            switch (request.QueryType)
            {
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
