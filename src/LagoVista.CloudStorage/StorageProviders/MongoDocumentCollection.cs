using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class MongoDocumentCollection : IDocumentCollection
    {
        private static readonly ConcurrentDictionary<string, MongoClient> _clients = new ConcurrentDictionary<string, MongoClient>(StringComparer.Ordinal);
        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly string _collectionName;

        public MongoDocumentCollection(string connectionString, string databaseName, string collectionName)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (String.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            MongoBsonSerialization.Configure();
            _connectionString = connectionString;
            _databaseName = databaseName;
            _collectionName = collectionName;
        }

        public async Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));
            var find = GetCollection<TDocument>().Find(query);
            if (sort != null) find = find.Sort(Builders<TDocument>.Sort.Ascending(ToObjectExpression(sort)));
            var items = await find.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Limit(listRequest.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
            return ListResponse<TDocument>.Create(listRequest, items);
        }

        public async Task<ListResponse<TProjection>> QueryAsync<TDocument, TProjection, TSort>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, Expression<Func<TDocument, TSort>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));
            var find = GetCollection<TDocument>().Find(query);
            if (sort != null) find = find.Sort(Builders<TDocument>.Sort.Ascending(ToObjectExpression(sort)));
            var items = await find.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Limit(listRequest.PageSize).Project(projection).ToListAsync(cancellationToken).ConfigureAwait(false);
            return ListResponse<TProjection>.Create(listRequest, items);
        }

        public async Task<IEnumerable<TProjection>> QueryAsync<TDocument, TProjection>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            return await GetCollection<TDocument>().Find(query).Project(projection).ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<TResult>> QueryAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            switch (request.QueryType)
            {
                case DocumentQueryType.CustomerIndustryNicheSalesStageCounts:
                    return await QueryCustomerIndustryNicheSalesStageCountsAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                case DocumentQueryType.EntityPreparationCandidateById:
                case DocumentQueryType.EntityPreparationCandidatesByType:
                case DocumentQueryType.IncompleteEntityPreparationCandidatesByType:
                    return await QueryEntityPreparationCandidatesAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                case DocumentQueryType.EntityListItems:
                case DocumentQueryType.EntityListHeaders:
                case DocumentQueryType.EntityListCategories:
                    return await QueryEntityListAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                case DocumentQueryType.EntityUtilsDocumentsByType:
                case DocumentQueryType.EntityUtilsDocumentById:
                    return await QueryEntityUtilsDocumentsAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                case DocumentQueryType.EntityUtilsCountByType:
                    return await QueryEntityUtilsCountAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                default:
                    throw new NotSupportedException($"Registered document query '{request.QueryType}' is not implemented by the Mongo provider.");
            }
        }

        private IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class
        {
            var client = _clients.GetOrAdd(_connectionString, connectionString => new MongoClient(connectionString));
            return client.GetDatabase(_databaseName).GetCollection<TDocument>(_collectionName);
        }

        private IMongoCollection<BsonDocument> GetBsonCollection()
        {
            var client = _clients.GetOrAdd(_connectionString, connectionString => new MongoClient(connectionString));
            return client.GetDatabase(_databaseName).GetCollection<BsonDocument>(_collectionName);
        }

        private static Expression<Func<TDocument, object>> ToObjectExpression<TDocument, TSort>(Expression<Func<TDocument, TSort>> expression)
        {
            var body = Expression.Convert(expression.Body, typeof(object));
            return Expression.Lambda<Func<TDocument, object>>(body, expression.Parameters);
        }

        private async Task<IEnumerable<TResult>> QueryCustomerIndustryNicheSalesStageCountsAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var orgId = request.GetRequired<string>("orgId");
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument { { "EntityType", "CustomerEntity" }, { "OwnerOrganization.Id", orgId } }),
                new BsonDocument("$group", new BsonDocument { { "_id", new BsonDocument { { "Industry", "$Industry" }, { "IndustryNiche", "$IndustryNiche" }, { "SalesStage", "$SalesStage" } } }, { "CountLeads", new BsonDocument("$sum", 1) } }),
                new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "Industry", "$_id.Industry" }, { "IndustryNiche", "$_id.IndustryNiche" }, { "SalesStage", "$_id.SalesStage" }, { "CountLeads", 1 } })
            };
            return Deserialize<TResult>(await GetBsonCollection().Aggregate(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false));
        }

        private async Task<IEnumerable<TResult>> QueryEntityPreparationCandidatesAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var match = new BsonDocument { { "EntityType", request.GetRequired<string>("entityType") }, { "OwnerOrganization.Id", request.GetRequired<string>("orgId") } };
            if (request.QueryType == DocumentQueryType.EntityPreparationCandidateById) match.Add("_id", request.GetRequired<string>("entityId"));
            if (request.QueryType == DocumentQueryType.IncompleteEntityPreparationCandidatesByType) match.Add("MasterStatus.IsProductionReady", new BsonDocument("$ne", true));
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", match),
                new BsonDocument("$sort", new BsonDocument("Name", 1)),
                new BsonDocument("$project", new BsonDocument { { "_id", 1 }, { "EntityType", 1 }, { "Name", 1 }, { "Key", 1 }, { "Description", 1 }, { "Icon", 1 }, { "Category", 1 }, { "IsDraft", 1 }, { "IsDeprecated", 1 }, { "MasterStatus", 1 }, { "ReadinessStatus", 1 }, { "CreationDate", 1 }, { "LastUpdatedDate", 1 }, { "Revision", 1 }, { "ChecklistStatus", 1 }, { "ReadinessChecks", 1 } })
            };
            if (request.QueryType == DocumentQueryType.EntityPreparationCandidateById) pipeline.Add(new BsonDocument("$limit", 1));
            else if (request.QueryType == DocumentQueryType.IncompleteEntityPreparationCandidatesByType) pipeline.Add(new BsonDocument("$limit", Math.Min(request.GetRequired<int>("maxItems"), 5000)));
            return Deserialize<TResult>(await GetBsonCollection().Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false));
        }

        private async Task<IEnumerable<TResult>> QueryEntityUtilsDocumentsAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var filter = request.QueryType == DocumentQueryType.EntityUtilsDocumentById
                ? new BsonDocument("_id", request.GetRequired<string>("entityId"))
                : new BsonDocument
                {
                    { "EntityType", request.GetRequired<string>("entityType") },
                    { "OwnerOrganization.Id", request.GetRequired<string>("orgId") }
                };

            var find = GetBsonCollection().Find(filter);
            if (request.QueryType == DocumentQueryType.EntityUtilsDocumentsByType)
                find = find.Sort(new BsonDocument("Name", 1));

            var documents = await find.Limit(request.QueryType == DocumentQueryType.EntityUtilsDocumentById ? 1 : 0).ToListAsync(cancellationToken).ConfigureAwait(false);

            if (typeof(TResult) == typeof(JObject))
                return documents.Select(ToJObject).Cast<TResult>().ToList();

            return Deserialize<TResult>(documents);
        }

        private async Task<IEnumerable<TResult>> QueryEntityUtilsCountAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var filter = new BsonDocument
            {
                { "EntityType", request.GetRequired<string>("entityType") },
                { "OwnerOrganization.Id", request.GetRequired<string>("orgId") }
            };
            var count = await GetBsonCollection().CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            var result = new DocumentCountResult { Count = checked((int)count) };
            return new[] { (TResult)(object)result };
        }

        private static JObject ToJObject(BsonDocument document)
        {
            var clone = document.DeepClone().AsBsonDocument;
            if (clone.TryGetValue("_id", out var id))
            {
                clone.Remove("_id");
                clone.InsertAt(0, new BsonElement("id", id));
            }
            return JObject.Parse(clone.ToJson());
        }

        private async Task<IEnumerable<TResult>> QueryEntityListAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var clauses = new BsonArray
            {
                new BsonDocument("EntityType", request.GetRequired<string>("entityType")),
                new BsonDocument("$or", new BsonArray { new BsonDocument("IsPublic", true), new BsonDocument("OwnerOrganization.Id", request.GetRequired<string>("orgId")) })
            };
            if (!request.GetRequired<bool>("showDeleted")) clauses.Add(new BsonDocument("$or", new BsonArray { new BsonDocument("IsDeleted", new BsonDocument("$exists", false)), new BsonDocument("IsDeleted", false) }));
            if (!request.GetRequired<bool>("showDrafts")) clauses.Add(new BsonDocument("$or", new BsonArray { new BsonDocument("IsDraft", new BsonDocument("$exists", false)), new BsonDocument("IsDraft", false) }));
            if (request.QueryType == DocumentQueryType.EntityListCategories)
            {
                clauses.Add(new BsonDocument("Category", new BsonDocument("$exists", true)));
                clauses.Add(new BsonDocument("Category.Key", new BsonDocument("$exists", true)));
            }
            else
            {
                AddIfPresent(clauses, "Category.Key", request.GetRequired<string>("categoryKey"));
                AddIfPresent(clauses, "Status.Key", request.GetRequired<string>("statusKey"));
                AddIfPresent(clauses, "Labels.Key", request.GetRequired<string>("labelKey"));
                var searchText = request.GetRequired<string>("searchText");
                if (!String.IsNullOrWhiteSpace(searchText)) clauses.Add(new BsonDocument("Name", new BsonRegularExpression(Regex.Escape(searchText), "i")));
            }
            var pipeline = new List<BsonDocument> { new BsonDocument("$match", new BsonDocument("$and", clauses)) };
            if (request.QueryType == DocumentQueryType.EntityListCategories)
            {
                pipeline.Add(new BsonDocument("$group", new BsonDocument("_id", new BsonDocument { { "Id", "$Category.Id" }, { "Key", "$Category.Key" }, { "Text", "$Category.Text" } } )));
                pipeline.Add(new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "Id", "$_id.Id" }, { "Key", "$_id.Key" }, { "Text", "$_id.Text" } }));
                pipeline.Add(new BsonDocument("$sort", new BsonDocument("Text", 1)));
            }
            else
            {
                var orderBy = (OrderByTypes)request.GetRequired<int>("orderBy");
                var sortField = orderBy == OrderByTypes.Rating ? "Stars" : orderBy == OrderByTypes.CreationDate ? "CreationDate" : orderBy == OrderByTypes.LastUpdateDate ? "LastUpdatedDate" : "Name";
                pipeline.Add(new BsonDocument("$sort", new BsonDocument(sortField, request.GetRequired<bool>("descending") ? -1 : 1)));
                var pageIndex = Math.Max(1, request.GetRequired<int>("pageIndex"));
                var pageSize = Math.Max(1, request.GetRequired<int>("pageSize"));
                pipeline.Add(new BsonDocument("$skip", (pageIndex - 1) * pageSize));
                pipeline.Add(new BsonDocument("$limit", pageSize));
                if (request.QueryType == DocumentQueryType.EntityListItems)
                    pipeline.Add(new BsonDocument("$project", new BsonDocument { { "_id", 1 }, { "Icon", 1 }, { "Name", 1 }, { "Key", 1 }, { "IsPublic", 1 }, { "IsDraft", 1 }, { "IsDeleted", 1 }, { "Category", "$Category.Text" }, { "Stars", 1 }, { "RatingsCount", 1 }, { "Labels", 1 }, { "Status", 1 } }));
                else
                    pipeline.Add(new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "Id", "$_id" }, { "Key", 1 }, { "Text", "$Name" } }));
            }
            return Deserialize<TResult>(await GetBsonCollection().Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false));
        }

        private static void AddIfPresent(BsonArray clauses, string field, string value)
        {
            if (!String.IsNullOrWhiteSpace(value)) clauses.Add(new BsonDocument(field, value));
        }

        private static IEnumerable<TResult> Deserialize<TResult>(IEnumerable<BsonDocument> documents) where TResult : class
        {
            var items = new List<TResult>();
            foreach (var document in documents) items.Add(BsonSerializer.Deserialize<TResult>(document));
            return items;
        }
    }
}
