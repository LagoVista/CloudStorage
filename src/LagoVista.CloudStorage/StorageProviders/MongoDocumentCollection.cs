using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    /// <summary>
    /// Provider-neutral document collection operations backed by MongoDB.
    /// Mongo's standard CLR Id convention is intentional: during Cosmos-to-Mongo migration,
    /// the Cosmos "id" value must become Mongo "_id" rather than retaining a duplicate "id" field.
    /// </summary>
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

            var collection = GetCollection<TDocument>();
            var find = collection.Find(query);
            if (sort != null) find = find.Sort(Builders<TDocument>.Sort.Ascending(ToObjectExpression(sort)));
            var items = await find.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Limit(listRequest.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
            return ListResponse<TDocument>.Create(listRequest, items);
        }

        public async Task<ListResponse<TProjection>> QueryAsync<TDocument, TProjection, TSort>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, Expression<Func<TDocument, TSort>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var collection = GetCollection<TDocument>();
            var find = collection.Find(query);
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
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var body = Expression.Convert(expression.Body, typeof(object));
            return Expression.Lambda<Func<TDocument, object>>(body, expression.Parameters);
        }

        private async Task<IEnumerable<TResult>> QueryCustomerIndustryNicheSalesStageCountsAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var orgId = request.GetRequired<string>("orgId");
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "EntityType", "CustomerEntity" },
                    { "OwnerOrganization.Id", orgId }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "Industry", "$Industry" },
                            { "IndustryNiche", "$IndustryNiche" },
                            { "SalesStage", "$SalesStage" }
                        }
                    },
                    { "CountLeads", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "Industry", "$_id.Industry" },
                    { "IndustryNiche", "$_id.IndustryNiche" },
                    { "SalesStage", "$_id.SalesStage" },
                    { "CountLeads", 1 }
                })
            };

            var documents = await GetBsonCollection().Aggregate(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false);
            return Deserialize<TResult>(documents);
        }

        private async Task<IEnumerable<TResult>> QueryEntityPreparationCandidatesAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var entityType = request.GetRequired<string>("entityType");
            var orgId = request.GetRequired<string>("orgId");
            var match = new BsonDocument
            {
                { "EntityType", entityType },
                { "OwnerOrganization.Id", orgId }
            };

            if (request.QueryType == DocumentQueryType.EntityPreparationCandidateById)
                match.Add("_id", request.GetRequired<string>("entityId"));

            if (request.QueryType == DocumentQueryType.IncompleteEntityPreparationCandidatesByType)
                match.Add("MasterStatus.IsProductionReady", new BsonDocument("$ne", true));

            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", match),
                new BsonDocument("$sort", new BsonDocument("Name", 1)),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 1 },
                    { "EntityType", 1 },
                    { "Name", 1 },
                    { "Key", 1 },
                    { "Description", 1 },
                    { "Icon", 1 },
                    { "Category", 1 },
                    { "IsDraft", 1 },
                    { "IsDeprecated", 1 },
                    { "MasterStatus", 1 },
                    { "ReadinessStatus", 1 },
                    { "CreationDate", 1 },
                    { "LastUpdatedDate", 1 },
                    { "Revision", 1 },
                    { "ChecklistStatus", 1 },
                    { "ReadinessChecks", 1 }
                })
            };

            var maxItems = request.QueryType == DocumentQueryType.EntityPreparationCandidateById ? 1 : Math.Min(request.GetRequired<int>("maxItems"), 5000);
            pipeline.Add(new BsonDocument("$limit", maxItems));

            var documents = await GetBsonCollection().Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false);
            return Deserialize<TResult>(documents);
        }

        private static IEnumerable<TResult> Deserialize<TResult>(IEnumerable<BsonDocument> documents) where TResult : class
        {
            var items = new List<TResult>();
            foreach (var document in documents) items.Add(BsonSerializer.Deserialize<TResult>(document));
            return items;
        }
    }
}
