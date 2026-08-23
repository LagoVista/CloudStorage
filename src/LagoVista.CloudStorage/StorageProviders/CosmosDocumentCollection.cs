using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class CosmosDocumentCollection : IDocumentCollection
    {
        private readonly ICosmosClientProvider _cosmosClientProvider;
        private readonly string _endpoint;
        private readonly string _sharedKey;
        private readonly string _databaseName;
        private readonly string _collectionName;

        public CosmosDocumentCollection(ICosmosClientProvider cosmosClientProvider, string endpoint, string sharedKey, string databaseName, string collectionName)
        {
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _sharedKey = sharedKey ?? throw new ArgumentNullException(nameof(sharedKey));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        }

        public async Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TDocument>().Where(query);
            if (sort != null) linqQuery = linqQuery.OrderBy(sort);
            linqQuery = linqQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize);

            var items = new List<TDocument>();
            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    items.AddRange(response);
                }
            }

            return ListResponse<TDocument>.Create(listRequest, items);
        }

        public async Task<ListResponse<TProjection>> QueryAsync<TDocument, TProjection, TSort>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, Expression<Func<TDocument, TSort>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TDocument>().Where(query);
            if (sort != null) linqQuery = linqQuery.OrderBy(sort);

            var projectedQuery = linqQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize).Select(projection);
            var items = new List<TProjection>();
            using (var iterator = projectedQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    items.AddRange(response);
                }
            }

            return ListResponse<TProjection>.Create(listRequest, items);
        }

        public async Task<IEnumerable<TProjection>> QueryAsync<TDocument, TProjection>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (projection == null) throw new ArgumentNullException(nameof(projection));

            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var projectedQuery = container.GetItemLinqQueryable<TDocument>().Where(query).Select(projection);
            var items = new List<TProjection>();
            using (var iterator = projectedQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    items.AddRange(response);
                }
            }

            return items;
        }

        public async Task<IEnumerable<TResult>> QueryAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var query = CreateQueryDefinition(request);
            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var iterator = container.GetItemQueryIterator<TResult>(query);
            var items = new List<TResult>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                items.AddRange(response);
            }

            return items;
        }

        private static QueryDefinition CreateQueryDefinition(DocumentQueryRequest request)
        {
            switch (request.QueryType)
            {
                case DocumentQueryType.CustomerIndustryNicheSalesStageCounts:
                    return new QueryDefinition("SELECT c.Industry, c.IndustryNiche, c.SalesStage, COUNT(c.id) AS CountLeads FROM c WHERE c.EntityType = 'CustomerEntity' AND c.OwnerOrganization.Id = @orgId GROUP BY c.Industry, c.IndustryNiche, c.SalesStage").WithParameter("@orgId", request.GetRequired<string>("orgId"));

                case DocumentQueryType.EntityPreparationCandidateById:
                    return CreateEntityPreparationCandidateByIdQuery(request);

                case DocumentQueryType.EntityPreparationCandidatesByType:
                    return CreateEntityPreparationCandidatesByTypeQuery(request, false);

                case DocumentQueryType.IncompleteEntityPreparationCandidatesByType:
                    return CreateEntityPreparationCandidatesByTypeQuery(request, true);

                case DocumentQueryType.EntityListItems:
                case DocumentQueryType.EntityListHeaders:
                case DocumentQueryType.EntityListCategories:
                    return CreateEntityListQuery(request);

                default:
                    throw new NotSupportedException($"Registered document query '{request.QueryType}' is not implemented by the Cosmos provider.");
            }
        }

        private static QueryDefinition CreateEntityPreparationCandidateByIdQuery(DocumentQueryRequest request)
        {
            var sql = $"SELECT TOP 1 {EntityPreparationProjection} FROM c WHERE c.EntityType = @entityType AND c.id = @entityId AND c.OwnerOrganization.Id = @orgId";
            return new QueryDefinition(sql).WithParameter("@entityType", request.GetRequired<string>("entityType")).WithParameter("@entityId", request.GetRequired<string>("entityId")).WithParameter("@orgId", request.GetRequired<string>("orgId"));
        }

        private static QueryDefinition CreateEntityPreparationCandidatesByTypeQuery(DocumentQueryRequest request, bool incompleteOnly)
        {
            var incompleteClause = incompleteOnly ? " AND (NOT IS_DEFINED(c.MasterStatus) OR IS_NULL(c.MasterStatus) OR NOT IS_DEFINED(c.MasterStatus.IsProductionReady) OR IS_NULL(c.MasterStatus.IsProductionReady) OR c.MasterStatus.IsProductionReady != true)" : String.Empty;
            var topClause = incompleteOnly ? $"TOP {Math.Min(request.GetRequired<int>("maxItems"), 5000)} " : String.Empty;
            var sql = $"SELECT {topClause}{EntityPreparationProjection} FROM c WHERE c.EntityType = @entityType AND c.OwnerOrganization.Id = @orgId{incompleteClause} ORDER BY c.Name ASC";
            return new QueryDefinition(sql).WithParameter("@entityType", request.GetRequired<string>("entityType")).WithParameter("@orgId", request.GetRequired<string>("orgId"));
        }

        private static QueryDefinition CreateEntityListQuery(DocumentQueryRequest request)
        {
            var queryType = request.QueryType;
            var sql = queryType == DocumentQueryType.EntityListItems
                ? @"SELECT VALUE {""id"": c.id, ""Icon"": c.Icon, ""Name"": c.Name, ""Key"": c.Key, ""IsPublic"": c.IsPublic, ""IsDraft"": c.IsDraft, ""IsDeleted"": c.IsDeleted, ""Category"": c.Category.Text, ""Stars"": c.Stars, ""RatingsCount"": c.RatingsCount, ""Labels"": c.Labels, ""Status"": c.Status} FROM c"
                : queryType == DocumentQueryType.EntityListHeaders
                    ? @"SELECT VALUE {""Id"": c.id, ""Key"": c.Key, ""Text"": c.Name} FROM c"
                    : @"SELECT DISTINCT VALUE {""Id"": c.Category.Id, ""Key"": c.Category.Key, ""Text"": c.Category.Text} FROM c";

            sql += " WHERE c.EntityType = @entityType AND (c.IsPublic = true OR c.OwnerOrganization.Id = @orgId)";

            if (queryType == DocumentQueryType.EntityListCategories)
                sql += " AND IS_DEFINED(c.Category) AND IS_DEFINED(c.Category.Key)";

            if (!request.GetRequired<bool>("showDeleted"))
                sql += " AND (NOT IS_DEFINED(c.IsDeleted) OR c.IsDeleted = false)";
            if (!request.GetRequired<bool>("showDrafts"))
                sql += " AND (NOT IS_DEFINED(c.IsDraft) OR c.IsDraft = false)";

            if (queryType != DocumentQueryType.EntityListCategories)
            {
                var categoryKey = request.GetRequired<string>("categoryKey");
                var statusKey = request.GetRequired<string>("statusKey");
                var labelKey = request.GetRequired<string>("labelKey");
                var searchText = request.GetRequired<string>("searchText");

                if (!String.IsNullOrWhiteSpace(categoryKey)) sql += " AND c.Category.Key = @categoryKey";
                if (!String.IsNullOrWhiteSpace(statusKey)) sql += " AND c.Status.Key = @statusKey";
                if (!String.IsNullOrWhiteSpace(labelKey)) sql += " AND ARRAY_CONTAINS(c.Labels, {\"Key\": @labelKey}, true)";
                if (!String.IsNullOrWhiteSpace(searchText)) sql += " AND CONTAINS(c.Name, @searchText, true)";
            }

            if (queryType == DocumentQueryType.EntityListCategories)
            {
                sql += " ORDER BY c.Category.Text";
            }
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

            if (queryType != DocumentQueryType.EntityListCategories)
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
