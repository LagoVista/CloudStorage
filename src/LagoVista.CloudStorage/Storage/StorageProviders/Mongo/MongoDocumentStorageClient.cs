using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Models.Storage;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    /// <summary>
    /// Mongo-specific document persistence. Common repository concerns such as validation,
    /// caching, dependency processing, audit preparation, and cache invalidation stay above
    /// this boundary so they execute identically regardless of the selected provider.
    /// </summary>
    public sealed partial class MongoDocumentStorageClient : IMongoDocumentStorageClient
    {
        private readonly IMongoDocumentStorageConnectionSettings _settings;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;
        private readonly IMongoStorageClientFactory _clientFactory;

        public MongoDocumentStorageClient(
            IMongoDocumentStorageConnectionSettings settings,
            IDocumentCollectionNameResolver collectionNameResolver,
            IMongoStorageClientFactory clientFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _collectionNameResolver = collectionNameResolver ?? throw new ArgumentNullException(nameof(collectionNameResolver));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            MongoBsonSerialization.Configure();
        }

        public async Task<DocumentStorageWriteResult> UpsertRawDocumentAsync(string entityType, string id, string json, string expectedETag = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));
            if (String.IsNullOrWhiteSpace(json)) throw new ArgumentException("Document JSON is required.", nameof(json));

            if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName))
                throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            var document = BsonDocument.Parse(json);

            document.Remove("id");
            document["_id"] = id;
            document["EntityType"] = entityType;

            var newETag = CreateETag();
            document["ETag"] = newETag;

            var filter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("_id", id), Builders<BsonDocument>.Filter.Eq("EntityType", entityType));

            if (!String.IsNullOrWhiteSpace(expectedETag))
                filter &= Builders<BsonDocument>.Filter.Eq("ETag", expectedETag);

            var result = await GetBsonCollection(collectionName).ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = String.IsNullOrWhiteSpace(expectedETag) }, cancellationToken).ConfigureAwait(false);

            if (!String.IsNullOrWhiteSpace(expectedETag) && result.MatchedCount == 0)
                throw new ContentModifiedException { EntityType = entityType, Id = id };

            return new DocumentStorageWriteResult
            {
                ETag = newETag,
                StatusCode = result.MatchedCount > 0 ? 200 : 201,
                RequestCharge = null
            };
        }

        public async Task DeleteDocumentAsync(string entityType, string id, string partitionKey = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));

            if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName))
                throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            var collection = GetBsonCollection(collectionName);
            var filter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("_id", id), Builders<BsonDocument>.Filter.Eq("EntityType", entityType));

            var result = await collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);

            if (result.DeletedCount == 0)
                throw new RecordNotFoundException(entityType, id);
        }

        public async Task<DocumentPage<TProjection>> GetDocumentPageAsync<TProjection>(string entityType = null, string continuationToken = null, int pageSize = 100, CancellationToken cancellationToken = default) where TProjection : class
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var collectionName = String.IsNullOrWhiteSpace(entityType)
                ? _collectionNameResolver.GetFallback(_settings.DatabaseName)
                : _collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var resolvedCollectionName)
                    ? resolvedCollectionName
                    : throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            var collection = GetBsonCollection(collectionName);
            var filters = new List<FilterDefinition<BsonDocument>>();

            if (!String.IsNullOrWhiteSpace(entityType))
                filters.Add(Builders<BsonDocument>.Filter.Eq("EntityType", entityType));

            if (!String.IsNullOrWhiteSpace(continuationToken))
                filters.Add(Builders<BsonDocument>.Filter.Gt("_id", continuationToken));

            var filter = filters.Count == 0 ? Builders<BsonDocument>.Filter.Empty : Builders<BsonDocument>.Filter.And(filters);

            var documents = await collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Ascending("_id")).Limit(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

            var items = documents.Select(ToProjection<TProjection>).ToList();
            var nextToken = documents.Count == pageSize ? documents.Last()["_id"].AsString : null;

            return new DocumentPage<TProjection>
            {
                Items = items,
                ContinuationToken = nextToken
            };
        }

        private static TProjection ToProjection<TProjection>(BsonDocument document) where TProjection : class
        {
            if (typeof(TProjection) == typeof(JObject))
                return (TProjection)(object)ToJObject(document);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<TProjection>(ToJObject(document).ToString());
        }
   
        public async Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var priorETag = item.ETag;
            item.ETag = CreateETag();
            try
            {
                await GetCollection<TEntity>().InsertOneAsync(item).ConfigureAwait(false);
                return new OperationResponse<TEntity>(item);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                item.ETag = priorETag;
                throw new ContentModifiedException { EntityType = typeof(TEntity).Name, Id = item.Id };
            }
            catch
            {
                item.ETag = priorETag;
                throw;
            }
        }

        public async Task<OperationResponse<TEntity>> UpsertDocumentAsync<TEntity>(TEntity item, string eTag = null)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var priorETag = item.ETag;
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Where(entity => entity.Id == item.Id),
                Builders<TEntity>.Filter.Eq(entity => entity.EntityType, typeof(TEntity).Name));

            if (!String.IsNullOrWhiteSpace(eTag))
                filter &= Builders<TEntity>.Filter.Eq(entity => entity.ETag, eTag);

            item.ETag = CreateETag();
            try
            {
                var result = await GetCollection<TEntity>()
                    .ReplaceOneAsync(filter, item, new ReplaceOptions { IsUpsert = String.IsNullOrWhiteSpace(eTag) })
                    .ConfigureAwait(false);

                if (!String.IsNullOrWhiteSpace(eTag) && result.MatchedCount == 0)
                {
                    item.ETag = priorETag;
                    throw new ContentModifiedException { EntityType = typeof(TEntity).Name, Id = item.Id };
                }

                return new OperationResponse<TEntity>(item);
            }
            catch
            {
                if (item.ETag != priorETag && !String.IsNullOrWhiteSpace(eTag))
                    item.ETag = priorETag;
                throw;
            }
        }

        public async Task<TEntity> GetDocumentAsync<TEntity>(string id, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            var entity = await GetCollection<TEntity>()
                .Find(item => item.Id == id && item.EntityType == typeof(TEntity).Name)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (entity == null && throwOnNotFound)
                throw new RecordNotFoundException(typeof(TEntity).Name, id);

            return entity;
        }

        public Task<TEntity> GetDocumentAsync<TEntity>(string id, string partitionKey, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetDocumentAsync<TEntity>(id, throwOnNotFound);

        public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            DeleteDocumentAsync<TEntity>(id, null);

        public async Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id, string partitionKey)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            var document = await GetCollection<TEntity>()
                .FindOneAndDeleteAsync(item => item.Id == id && item.EntityType == typeof(TEntity).Name)
                .ConfigureAwait(false);

            if (document == null)
                throw new RecordNotFoundException(typeof(TEntity).Name, id);

            return new OperationResponse<TEntity>(document);
        }

        public async Task<OperationResponse<TEntity>> PatchDocumentAsync<TEntity>(PatchRequest request)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrWhiteSpace(request.Id)) throw new ArgumentException("Patch request id is required.", nameof(request));
            if (request.Steps == null || request.Steps.Count == 0) throw new ArgumentException("Patch request must contain at least one step.", nameof(request));

            var collection = GetCollection<TEntity>();
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq(item => item.Id.Value, request.Id),
                Builders<TEntity>.Filter.Eq(item => item.EntityType, typeof(TEntity).Name));

            if (!String.IsNullOrWhiteSpace(request.ETag))
                filter &= Builders<TEntity>.Filter.Eq(item => item.ETag, request.ETag);

            var updates = request.Steps.Select(CreatePatchUpdate<TEntity>).ToList();
            updates.Add(Builders<TEntity>.Update.Set(item => item.ETag, CreateETag()));
            var update = Builders<TEntity>.Update.Combine(updates);
            var entity = await collection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<TEntity> { ReturnDocument = ReturnDocument.After }).ConfigureAwait(false);

            if (entity == null)
            {
                if (!String.IsNullOrWhiteSpace(request.ETag))
                {
                    var exists = await collection.Find(item => item.Id == request.Id && item.EntityType == typeof(TEntity).Name)
                        .AnyAsync().ConfigureAwait(false);

                    if (exists) throw new ContentModifiedException { EntityType = typeof(TEntity).Name, Id = request.Id };
                }

                throw new RecordNotFoundException(typeof(TEntity).Name, request.Id);
            }

            return new OperationResponse<TEntity>(entity);
        }

        public async Task<InvokeResult> PatchDocumentAsync(string entityType, PatchRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrWhiteSpace(request.Id)) throw new ArgumentException("Patch request id is required.", nameof(request));
            if (request.Steps == null || request.Steps.Count == 0) throw new ArgumentException("Patch request must contain at least one step.", nameof(request));

            if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName))
                throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            var collection = GetBsonCollection(collectionName);

            var filter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("_id", request.Id), Builders<BsonDocument>.Filter.Eq("EntityType", entityType));

            if (!String.IsNullOrWhiteSpace(request.ETag))
                filter &= Builders<BsonDocument>.Filter.Eq("ETag", request.ETag);

            var updates = request.Steps.Select(CreateRuntimePatchUpdate).ToList();
            updates.Add(Builders<BsonDocument>.Update.Set("ETag", CreateETag()));

            var update = Builders<BsonDocument>.Update.Combine(updates);

            var result = await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                if (!String.IsNullOrWhiteSpace(request.ETag))
                {
                    var existsFilter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("_id", request.Id), Builders<BsonDocument>.Filter.Eq("EntityType", entityType));
                    var exists = await collection.Find(existsFilter).AnyAsync(cancellationToken).ConfigureAwait(false);

                    if (exists)
                        throw new ContentModifiedException { EntityType = entityType, Id = request.Id };
                }

                throw new RecordNotFoundException(entityType, request.Id);
            }

            return InvokeResult.Success;
        }

        private static UpdateDefinition<BsonDocument> CreateRuntimePatchUpdate(PatchStep step)
        {
            if (step == null) throw new ArgumentException("Patch request contains a null step.");

            var path = ToMongoPath(step);

            switch (step.Op)
            {
                case PatchOp.Set:
                case PatchOp.Add:
                    return Builders<BsonDocument>.Update.Set(path, ToBsonValue(step.Value));

                case PatchOp.Remove:
                    return Builders<BsonDocument>.Update.Unset(path);

                default:
                    throw new NotSupportedException($"Patch operation '{step.Op}' is not supported by the Mongo document client.");
            }
        }

        public async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Where(query),
                Builders<TEntity>.Filter.Eq(item => item.EntityType, typeof(TEntity).Name));
            return await GetCollection<TEntity>().Find(filter).ToListAsync().ConfigureAwait(false);
        }

        public async Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var filter = CreatePagedQueryFilter(query, listRequest);
            var items = await GetCollection<TEntity>().Find(filter)
                .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                .Limit(listRequest.PageSize)
                .ToListAsync()
                .ConfigureAwait(false);
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

            var filter = CreatePagedQueryFilter(query, listRequest);
            var find = GetCollection<TEntity>().Find(filter);
            var parameter = sort.Parameters[0];
            var objectSort = Expression.Lambda<Func<TEntity, object>>(
                Expression.Convert(sort.Body, typeof(object)),
                parameter);

            var sortDefinition = descending
                ? Builders<TEntity>.Sort.Descending(objectSort)
                : Builders<TEntity>.Sort.Ascending(objectSort);

            var sorted = find.Sort(sortDefinition);
            var items = await sorted
                .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                .Limit(listRequest.PageSize)
                .ToListAsync()
                .ConfigureAwait(false);
            return ListResponse<TEntity>.Create(listRequest, items);
        }

        private static FilterDefinition<TEntity> CreatePagedQueryFilter<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            var filters = new List<FilterDefinition<TEntity>>
            {
                Builders<TEntity>.Filter.Where(query),
                Builders<TEntity>.Filter.Eq(item => item.EntityType, typeof(TEntity).Name)
            };

            if (!listRequest.ShowDeleted)
            {
                filters.Add(Builders<TEntity>.Filter.Or(
                    Builders<TEntity>.Filter.Exists("IsDeleted", false),
                    Builders<TEntity>.Filter.Eq("IsDeleted", false)));
            }

            if (!listRequest.ShowDrafts)
            {
                filters.Add(Builders<TEntity>.Filter.Or(
                    Builders<TEntity>.Filter.Exists("IsDraft", false),
                    Builders<TEntity>.Filter.Eq("IsDraft", false)));
            }

            return Builders<TEntity>.Filter.And(filters);
        }

        public async Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(string entityType, DocumentQueryRequest request, CancellationToken cancellationToken = default)
            where TResult : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required for Mongo known-query routing.", nameof(entityType));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName))
                throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            var collection = GetBsonCollection(collectionName);
            switch (request.QueryType)
            {
                case DocumentQueryType.EntityUtilsCompletedChecklistCandidates:
                    return await QueryCompletedChecklistCandidatesAsync<TResult>(collection, request, cancellationToken).ConfigureAwait(false);

                case DocumentQueryType.EntityUtilsCompletedChecklistCount:
                    {
                        var count = await CountCompletedChecklistCandidatesAsync(collection, request, cancellationToken).ConfigureAwait(false);
                        return new[] { (TResult)(object)new DocumentCountResult { Count = checked((int)count) } };
                    }

                case DocumentQueryType.EntityUtilsDocumentsByFieldValue:
                    return await QueryEntityUtilsByFieldValueAsync<TResult>(collection, request, cancellationToken).ConfigureAwait(false);

                case DocumentQueryType.EntityUtilsDocumentsByStatusIds:
                    return await QueryEntityUtilsByStatusIdsAsync<TResult>(collection, request, cancellationToken).ConfigureAwait(false);

                case DocumentQueryType.EntityUtilsDocumentsWithEmptyField:
                    return await QueryEntityUtilsWithEmptyFieldAsync<TResult>(collection, request, cancellationToken).ConfigureAwait(false);
                    
                case DocumentQueryType.CustomerIndustryNicheSalesStageCounts:
                    return Deserialize<TResult>(await collection.Aggregate<BsonDocument>(new BsonDocument[]
                    {
                        new BsonDocument("$match", new BsonDocument { { "EntityType", "CustomerEntity" }, { "OwnerOrganization.Id", request.GetRequired<string>("orgId") } }),
                        new BsonDocument("$group", new BsonDocument { { "_id", new BsonDocument { { "Industry", "$Industry" }, { "IndustryNiche", "$IndustryNiche" }, { "SalesStage", "$SalesStage" } } }, { "CountLeads", new BsonDocument("$sum", 1) } }),
                        new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "Industry", "$_id.Industry" }, { "IndustryNiche", "$_id.IndustryNiche" }, { "SalesStage", "$_id.SalesStage" }, { "CountLeads", 1 } })
                    }).ToListAsync(cancellationToken).ConfigureAwait(false));

                case DocumentQueryType.EntityUtilsDocumentsByType:
                case DocumentQueryType.EntityUtilsDocumentById:
                    return await QueryEntityUtilsDocumentsAsync<TResult>(collection, request, cancellationToken).ConfigureAwait(false);

                case DocumentQueryType.EntityUtilsCountByType:
                    {
                        var count = await collection.CountDocumentsAsync(new BsonDocument
                        {
                            { "EntityType", request.GetRequired<string>("entityType") },
                            { "OwnerOrganization.Id", request.GetRequired<string>("orgId") }
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);
                            return new[] { (TResult)(object)new DocumentCountResult { Count = checked((int)count) } };
                    }
                case DocumentQueryType.EntityPreparationCandidateById:
                case DocumentQueryType.EntityPreparationCandidatesByType:
                case DocumentQueryType.IncompleteEntityPreparationCandidatesByType:
                    return await QueryPreparationAsync<TResult>(collection, request, cancellationToken).ConfigureAwait(false);

                case DocumentQueryType.EntityListItems:
                case DocumentQueryType.EntityListHeaders:
                case DocumentQueryType.EntityListCategories:
                    return await QueryEntityListAsync<TResult>(collection, request, cancellationToken).ConfigureAwait(false);

                default:
                    throw new NotSupportedException($"Registered document query '{request.QueryType}' is not implemented by the Mongo provider.");
            }
        }

        private static Task<long> CountCompletedChecklistCandidatesAsync(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken)
        {
            return collection.CountDocumentsAsync(BuildCompletedChecklistFilter(request), cancellationToken: cancellationToken);
        }

        private static async Task<IEnumerable<TResult>> QueryCompletedChecklistCandidatesAsync<TResult>(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var maxItems = Math.Min(request.GetRequired<int>("maxItems"), 5000);
            var filter = BuildCompletedChecklistFilter(request);

            var documents = await collection
                .Find(filter)
                .Sort(Builders<BsonDocument>.Sort.Ascending("Name"))
                .Limit(maxItems)
                .Project(new BsonDocument
                {
            { "_id", 1 },
            { "EntityType", 1 },
            { "Name", 1 },
            { "Key", 1 },
            { "Description", 1 },
            { "ChecklistStatus", 1 }
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Deserialize<TResult>(documents);
        }
        private static FilterDefinition<BsonDocument> BuildCompletedChecklistFilter(DocumentQueryRequest request)
        {
            var stepKeys = request.GetRequired<List<string>>("stepKeys");

            var filters = new List<FilterDefinition<BsonDocument>>
    {
        Builders<BsonDocument>.Filter.Eq("EntityType", request.GetRequired<string>("entityType")),
        Builders<BsonDocument>.Filter.Eq("OwnerOrganization.Id", request.GetRequired<string>("orgId"))
    };

            foreach (var stepKey in stepKeys)
            {
                filters.Add(Builders<BsonDocument>.Filter.ElemMatch<BsonValue>("ChecklistStatus",
                    new BsonDocument
                    {
                { "StepKey", stepKey },
                { "LastRun", new BsonDocument { { "$exists", true }, { "$ne", BsonNull.Value } } }
                    }));
            }

            return Builders<BsonDocument>.Filter.And(filters);
        }

        private static async Task<IEnumerable<TResult>> QueryEntityUtilsByFieldValueAsync<TResult>(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("EntityType", request.GetRequired<string>("entityType")),
                Builders<BsonDocument>.Filter.Eq("OwnerOrganization.Id", request.GetRequired<string>("orgId")),
                Builders<BsonDocument>.Filter.Eq(request.GetRequired<string>("fieldName"), request.GetRequired<string>("value")));

            var documents = await collection.Find(filter).Project(new BsonDocument { { "_id", 1 } }).ToListAsync(cancellationToken).ConfigureAwait(false);

            return Deserialize<TResult>(documents);
        }

        private static async Task<IEnumerable<TResult>> QueryEntityUtilsByStatusIdsAsync<TResult>(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var statusIds = request.GetRequired<List<string>>("statusIds");
            var maxItems = Math.Min(request.GetRequired<int>("maxItems"), 5000);

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("EntityType", request.GetRequired<string>("entityType")),
                Builders<BsonDocument>.Filter.Eq("OwnerOrganization.Id", request.GetRequired<string>("orgId")),
                Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Exists("Status", false),
                    Builders<BsonDocument>.Filter.Eq("Status", BsonNull.Value),
                    Builders<BsonDocument>.Filter.Exists("Status.Id", false),
                    Builders<BsonDocument>.Filter.Eq("Status.Id", BsonNull.Value),
                    Builders<BsonDocument>.Filter.In("Status.Id", statusIds)));

            var documents = await collection
                .Find(filter)
                .Sort(Builders<BsonDocument>.Sort.Ascending("Name"))
                .Limit(maxItems)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (typeof(TResult) == typeof(JObject))
                return documents.Select(ToJObject).Cast<TResult>().ToList();

            return Deserialize<TResult>(documents);
        }

        private static async Task<IEnumerable<TResult>> QueryEntityUtilsWithEmptyFieldAsync<TResult>(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var fieldName = request.GetRequired<string>("fieldName");
            var maxItems = Math.Min(request.GetRequired<int>("maxItems"), 5000);

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("EntityType", request.GetRequired<string>("entityType")),
                Builders<BsonDocument>.Filter.Eq("OwnerOrganization.Id", request.GetRequired<string>("orgId")),
                Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Exists(fieldName, false),
                    Builders<BsonDocument>.Filter.Eq(fieldName, BsonNull.Value),
                    Builders<BsonDocument>.Filter.Eq(fieldName, String.Empty)));

            var documents = await collection
                .Find(filter)
                .Limit(maxItems)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (typeof(TResult) == typeof(JObject))
                return documents.Select(ToJObject).Cast<TResult>().ToList();

            return Deserialize<TResult>(documents);
        }

        private IMongoCollection<TEntity> GetCollection<TEntity>() where TEntity : class
        {
            var collectionName = _collectionNameResolver.Resolve(_settings.DatabaseName, typeof(TEntity), null);
            return _clientFactory.GetDatabase(_settings.BuildConnectionString(), _settings.DatabaseName).GetCollection<TEntity>(collectionName);
        }

        private IMongoCollection<BsonDocument> GetBsonCollection(string collectionName) =>
            _clientFactory.GetDatabase(_settings.BuildConnectionString(), _settings.DatabaseName).GetCollection<BsonDocument>(collectionName);

        private static UpdateDefinition<TEntity> CreatePatchUpdate<TEntity>(PatchStep step) where TEntity : class
        {
            if (step == null) throw new ArgumentException("Patch request contains a null step.");
            var path = ToMongoPath(step);
            switch (step.Op)
            {
                case PatchOp.Set:
                case PatchOp.Add:
                    return Builders<TEntity>.Update.Set(path, ToBsonValue(step.Value));
                case PatchOp.Remove:
                    return Builders<TEntity>.Update.Unset(path);
                default:
                    throw new NotSupportedException($"Patch operation '{step.Op}' is not supported by the Mongo document client.");
            }
        }

        private static string ToMongoPath(PatchStep step)
        {
            var path = !String.IsNullOrWhiteSpace(step.LogicalPath)
                ? step.LogicalPath.Trim().TrimStart('/')
                : step.CosmosPath?.Trim().TrimStart('/').Replace('/', '.');
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Patch step path is required.");
            if (String.Equals(path, "id", StringComparison.OrdinalIgnoreCase)) return "_id";
            return path;
        }

        private static BsonValue ToBsonValue(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null) return BsonNull.Value;
            return BsonDocument.Parse($"{{\"value\":{value.ToString(Formatting.None)}}}")["value"];
        }

        private static string CreateETag() => Guid.NewGuid().ToString("N");

        private static async Task<IEnumerable<TResult>> QueryEntityUtilsDocumentsAsync<TResult>(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var filter = new BsonDocument
            {
                { "EntityType", request.GetRequired<string>("entityType") },
                { "OwnerOrganization.Id", request.GetRequired<string>("orgId") }
            };
            if (request.QueryType == DocumentQueryType.EntityUtilsDocumentById) filter.Add("_id", request.GetRequired<string>("entityId"));
            var find = collection.Find(filter);
            if (request.QueryType == DocumentQueryType.EntityUtilsDocumentsByType) find = find.Sort(new BsonDocument("Name", 1));
            if (request.QueryType == DocumentQueryType.EntityUtilsDocumentById) find = find.Limit(1);
            var docs = await find.ToListAsync(cancellationToken).ConfigureAwait(false);
            if (typeof(TResult) == typeof(JObject)) return docs.Select(ToJObject).Cast<TResult>().ToList();
            return Deserialize<TResult>(docs);
        }

        private static async Task<IEnumerable<TResult>> QueryPreparationAsync<TResult>(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
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
            return Deserialize<TResult>(await collection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<IEnumerable<TResult>> QueryEntityListAsync<TResult>(IMongoCollection<BsonDocument> collection, DocumentQueryRequest request, CancellationToken cancellationToken) where TResult : class
        {
            var clauses = new BsonArray
            {
                new BsonDocument("EntityType", request.GetRequired<string>("entityType")),
                new BsonDocument("$or", new BsonArray { new BsonDocument("IsPublic", true), new BsonDocument("OwnerOrganization.Id", request.GetRequired<string>("orgId") ) })
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
                pipeline.Add(new BsonDocument("$group", new BsonDocument("_id", new BsonDocument { { "Id", "$Category.Id" }, { "Key", "$Category.Key" }, { "Text", "$Category.Text" } })));
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
            return Deserialize<TResult>(await collection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false));
        }

        private static void AddIfPresent(BsonArray clauses, string field, string value)
        {
            if (!String.IsNullOrWhiteSpace(value)) clauses.Add(new BsonDocument(field, value));
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

        private static IEnumerable<TResult> Deserialize<TResult>(IEnumerable<BsonDocument> documents) where TResult : class =>
            documents.Select(document => BsonSerializer.Deserialize<TResult>(document)).ToList();
    }
}
