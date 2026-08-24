using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json.Linq;
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

        private Container GetContainer()
        {
            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            return client.GetContainer(_databaseName, _collectionName);
        }

        public async Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var linqQuery = GetContainer().GetItemLinqQueryable<TDocument>().Where(query);
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

            var linqQuery = GetContainer().GetItemLinqQueryable<TDocument>().Where(query);
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

            var projectedQuery = GetContainer().GetItemLinqQueryable<TDocument>().Where(query).Select(projection);
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

        public async Task<JObject> GetDocumentAsync(string id, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));

            var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.id = @id").WithParameter("@id", id.Trim());
            using var iterator = GetContainer().GetItemQueryIterator<JObject>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                var document = response.FirstOrDefault();
                if (document != null) return document;
            }

            return null;
        }

        public async Task<IEnumerable<JObject>> QueryDocumentsAsync(DocumentFilterRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var query = CreateFilterQuery("SELECT * FROM c", request, includeSortAndLimit: true);
            using var iterator = GetContainer().GetItemQueryIterator<JObject>(query);
            var documents = new List<JObject>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                documents.AddRange(response.Where(item => item != null));
            }

            return documents;
        }

        public async Task<int> CountDocumentsAsync(DocumentFilterRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var query = CreateFilterQuery("SELECT VALUE COUNT(1) FROM c", request, includeSortAndLimit: false);
            using var iterator = GetContainer().GetItemQueryIterator<int>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                return response.FirstOrDefault();
            }

            return 0;
        }

        public async Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(KnownDocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var query = CreateKnownQueryDefinition(request);
            using var iterator = GetContainer().GetItemQueryIterator<TResult>(query);
            var items = new List<TResult>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                items.AddRange(response);
            }

            return items;
        }

        private static QueryDefinition CreateFilterQuery(string selectClause, DocumentFilterRequest request, bool includeSortAndLimit)
        {
            var predicates = new List<string>();
            var parameters = new List<KeyValuePair<string, object>>();
            var index = 0;

            foreach (var filter in request.Equals)
            {
                var field = ToCosmosField(filter.Key);
                var parameterName = $"@p{index++}";
                predicates.Add($"{field} = {parameterName}");
                parameters.Add(new KeyValuePair<string, object>(parameterName, filter.Value));
            }

            var sql = selectClause;
            if (predicates.Count > 0) sql += $" WHERE {String.Join(" AND ", predicates)}";

            if (includeSortAndLimit && !String.IsNullOrWhiteSpace(request.SortField))
                sql += $" ORDER BY {ToCosmosField(request.SortField)}{(request.SortDescending ? " DESC" : " ASC")}";

            if (includeSortAndLimit && request.Limit.HasValue)
                sql += $" OFFSET 0 LIMIT {request.Limit.Value}";

            var query = new QueryDefinition(sql);
            foreach (var parameter in parameters) query.WithParameter(parameter.Key, parameter.Value);
            return query;
        }

        private static string ToCosmosField(string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Document field name is required.", nameof(fieldName));
            var segments = fieldName.Split('.');
            foreach (var segment in segments)
            {
                if (String.IsNullOrWhiteSpace(segment) || segment.Any(ch => !(Char.IsLetterOrDigit(ch) || ch == '_')))
                    throw new ArgumentException($"Document field '{fieldName}' is not safe for a provider query.", nameof(fieldName));
            }
            return $"c.{String.Join(".", segments)}";
        }

        private static QueryDefinition CreateKnownQueryDefinition(KnownDocumentQueryRequest request)
        {
            switch (request.Query)
            {
                case KnownDocumentQuery.CustomerIndustryNicheSalesStageCounts:
                    return new QueryDefinition("SELECT c.Industry, c.IndustryNiche, c.SalesStage, COUNT(c.id) AS CountLeads FROM c WHERE c.EntityType = 'CustomerEntity' AND c.OwnerOrganization.Id = @orgId GROUP BY c.Industry, c.IndustryNiche, c.SalesStage").WithParameter("@orgId", request.GetRequired<string>("orgId"));

                case KnownDocumentQuery.EntityPreparationCandidateById:
                    return CreateEntityPreparationCandidateByIdQuery(request);

                case KnownDocumentQuery.EntityPreparationCandidatesByType:
                    return CreateEntityPreparationCandidatesByTypeQuery(request, false);

                case KnownDocumentQuery.IncompleteEntityPreparationCandidatesByType:
                    return CreateEntityPreparationCandidatesByTypeQuery(request, true);

                case KnownDocumentQuery.EntityListItems:
                case KnownDocumentQuery.EntityListHeaders:
                case KnownDocumentQuery.EntityListCategories:
                    return CreateEntityListQuery(request);

                default:
                    throw new NotSupportedException($"Known document query '{request.Query}' is not implemented by the Cosmos provider.");
            }
        }

        private static QueryDefinition CreateEntityPreparationCandidateByIdQuery(KnownDocumentQueryRequest request)
        {
            var sql = $"SELECT TOP 1 {EntityPreparationProjection} FROM c WHERE c.EntityType = @entityType AND c.id = @entityId AND c.OwnerOrganization.Id = @orgId";
            return new QueryDefinition(sql).WithParameter("@entityType", request.GetRequired<string>("entityType")).WithParameter("@entityId", request.GetRequired<string>("entityId")).WithParameter("@orgId", request.GetRequired<string>("orgId"));
        }

        private static QueryDefinition CreateEntityPreparationCandidatesByTypeQuery(KnownDocumentQueryRequest request, bool incompleteOnly)
        {
            var incompleteClause = incompleteOnly ? " AND (NOT IS_DEFINED(c.MasterStatus) OR IS_NULL(c.MasterStatus) OR NOT IS_DEFINED(c.MasterStatus.IsProductionReady) OR IS_NULL(c.MasterStatus.IsProductionReady) OR c.MasterStatus.IsProductionReady != true)" : String.Empty;
            var topClause = incompleteOnly ? $"TOP {Math.Min(request.GetRequired<int>("maxItems"), 5000)} " : String.Empty;
            var sql = $"SELECT {topClause}{EntityPreparationProjection} FROM c WHERE c.EntityType = @entityType AND c.OwnerOrganization.Id = @orgId{incompleteClause} ORDER BY c.Name ASC";
            return new QueryDefinition(sql).WithParameter("@entityType", request.GetRequired<string>("entityType")).WithParameter("@orgId", request.GetRequired<string>("orgId"));
        }

        private static QueryDefinition CreateEntityListQuery(KnownDocumentQueryRequest request)
        {
            var queryType = request.Query;
            var sql = queryType == KnownDocumentQuery.EntityListItems
                ? @"SELECT VALUE {""id"": c.id, ""Icon"": c.Icon, ""Name"": c.Name, ""Key"": c.Key, ""IsPublic"": c.IsPublic, ""IsDraft"": c.IsDraft, ""IsDeleted"": c.IsDeleted, ""Category"": c.Category.Text, ""Stars"": c.Stars, ""RatingsCount"": c.RatingsCount, ""Labels"": c.Labels, ""Status"": c.Status} FROM c"
                : queryType == KnownDocumentQuery.EntityListHeaders
                    ? @"SELECT VALUE {""Id"": c.id, ""Key"": c.Key, ""Text"": c.Name} FROM c"
                    : @"SELECT DISTINCT VALUE {""Id"": c.Category.Id, ""Key"": c.Category.Key, ""Text"": c.Category.Text} FROM c";

            sql += " WHERE c.EntityType = @entityType AND (c.IsPublic = true OR c.OwnerOrganization.Id = @orgId)";

            if (queryType == KnownDocumentQuery.EntityListCategories)
                sql += " AND IS_DEFINED(c.Category) AND IS_DEFINED(c.Category.Key)";

            if (!request.GetRequired<bool>("showDeleted"))
                sql += " AND (NOT IS_DEFINED(c.IsDeleted) OR c.IsDeleted = false)";
            if (!request.GetRequired<bool>("showDrafts"))
                sql += " AND (NOT IS_DEFINED(c.IsDraft) OR c.IsDraft = false)";

            if (queryType != KnownDocumentQuery.EntityListCategories)
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

            if (queryType == KnownDocumentQuery.EntityListCategories)
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

            if (queryType != KnownDocumentQuery.EntityListCategories)
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
