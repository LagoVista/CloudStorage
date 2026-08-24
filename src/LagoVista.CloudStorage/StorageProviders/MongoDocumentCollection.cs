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

        public async Task<JObject> GetDocumentAsync(string id, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));
            var document = await GetBsonCollection().Find(new BsonDocument("_id", id.Trim())).Limit(1).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            return document == null ? null : ToJObject(document);
        }

        public async Task<IEnumerable<JObject>> QueryDocumentsAsync(DocumentFilterRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var filter = BuildFilter(request);
            var find = GetBsonCollection().Find(filter);
            if (!String.IsNullOrWhiteSpace(request.SortField))
                find = find.Sort(new BsonDocument(ToMongoField(request.SortField), request.SortDescending ? -1 : 1));
            if (request.Limit.HasValue) find = find.Limit(request.Limit.Value);
            var documents = await find.ToListAsync(cancellationToken).ConfigureAwait(false);
            return documents.Select(ToJObject).ToList();
        }

        public async Task<int> CountDocumentsAsync(DocumentFilterRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = await GetBsonCollection().CountDocumentsAsync(BuildFilter(request), cancellationToken: cancellationToken).ConfigureAwait(false);
            return checked((int)count);
        }

        public async Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(KnownDocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            switch (request.Query)
            {
                case KnownDocumentQuery.CustomerIndustryNicheSalesStageCounts:
                    return await QueryCustomerIndustryNicheSalesStageCountsAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                case KnownDocumentQuery.EntityPreparationCandidateById:
                case KnownDocumentQuery.EntityPreparationCandidatesByType:
                case KnownDocumentQuery.IncompleteEntityPreparationCandidatesByType:
                    return await QueryEntityPreparationCandidatesAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                case KnownDocumentQuery.EntityListItems:
                case KnownDocumentQuery.EntityListHeaders:
                case KnownDocumentQuery.EntityListCategories:
                    return await QueryEntityListAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
                default:
                    throw new NotSupportedException($"Known document query '{request.Query}' is not implemented by the Mongo provider.");
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

        private static BsonDocument BuildFilter(DocumentFilterRequest request)
        {
            var filter = new BsonDocument();
            foreach (var item in request.Equals)
                filter[ToMongoField(item.Key)] = BsonValue.Create(item.Value);
            return filter;
        }

        private static string ToMongoField(string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Document field name is required.", nameof(fieldName));
            var segments = fieldName.Split('.');
            foreach (var segment in segments)
            {
                if (String.IsNullOrWhiteSpace(segment) || segment.Any(ch => !(Char.IsLetterOrDigit(ch) || ch == '_')))
                    throw new ArgumentException($"Document field '{fieldName}' is not safe for a provider query.", nameof(fieldName));
            }
            if (segments.Length == 1 && String.Equals(segments[0], "id", StringComparison.OrdinalIgnoreCase)) return "_id";
            return String.Join(".", segments);
        }

        private async Task<IEnumerable<TResult>> QueryCustomerIndustryNicheSalesStageCountsAsync<TResult>(KnownDocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
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

        private async Task<IEnumerable<TResult>> QueryEntityPreparationCandidatesAsync<TResult>(KnownDocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var match = new BsonDocument { { "EntityType", request.GetRequired<string>("entityType") }, { "OwnerOrganization.Id", request.GetRequired<string>("orgId") } };
            if (request.Query == KnownDocumentQuery.EntityPreparationCandidateById) match.Add("_id", request.GetRequired<string>("entityId"));
            if (request.Query == KnownDocumentQuery.IncompleteEntityPreparationCandidatesByType) match.Add("MasterStatus.IsProductionReady", new BsonDocument("$ne", true));
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", match),
                new BsonDocument("$sort", new BsonDocument("Name", 1)),
                new BsonDocument("$project", new BsonDocument { { "_id", 1 }, { "EntityType", 1 }, { "Name", 1 }, { "Key", 1 }, { "Description", 1 }, { "Icon", 1 }, { "Category", 1 }, { "IsDraft", 1 }, { "IsDeprecated", 1 }, { "MasterStatus", 1 }, { "ReadinessStatus", 1 }, { "CreationDate", 1 }, { "LastUpdatedDate", 1 }, { "Revision", 1 }, { "ChecklistStatus", 1 }, { "ReadinessChecks", 1 } })
            };
            if (request.Query == KnownDocumentQuery.EntityPreparationCandidateById) pipeline.Add(new BsonDocument("$limit", 1));
            else if (request.Query == KnownDocumentQuery.IncompleteEntityPreparationCandidatesByType) pipeline.Add(new BsonDocument("$limit", Math.Min(request.GetRequired<int>("maxItems"), 5000)));
            return Deserialize<TResult>(await GetBsonCollection().Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false));
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

        private async Task<IEnumerable<TResult>> QueryEntityListAsync<TResult>(KnownDocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var clauses = new BsonArray
            {
                new BsonDocument("EntityType", request.GetRequired<string>("entityType")),
                new BsonDocument("$or", new BsonArray { new BsonDocument("IsPublic", true), new BsonDocument("OwnerOrganization.Id", request.GetRequired<string>("orgId")) })
            };
            if (!request.GetRequired<bool>("showDeleted")) clauses.Add(new BsonDocument("$or", new BsonArray { new BsonDocument("IsDeleted", new BsonDocument("$exists", false)), new BsonDocument("IsDeleted", false) }));
            if (!request.GetRequired<bool>("showDrafts")) clauses.Add(new BsonDocument("$or", new BsonArray { new BsonDocument("IsDraft", new BsonDocument("$exists", false)), new BsonDocument("IsDraft", false) }));
            if (request.Query == KnownDocumentQuery.EntityListCategories)
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
            if (request.Query == KnownDocumentQuery.EntityListCategories)
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
                if (request.Query == KnownDocumentQuery.EntityListItems)
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
