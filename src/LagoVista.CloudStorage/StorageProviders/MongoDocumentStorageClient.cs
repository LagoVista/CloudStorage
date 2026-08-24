using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class MongoDocumentStorageClient : IMongoDocumentStorageClient
    {
        private readonly IMongoConnectionSettings _settings;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IDependencyManager _dependencyManager;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;
        private readonly IMongoStorageClientFactory _clientFactory;

        public MongoDocumentStorageClient(
            IMongoConnectionSettings settings,
            IAdminLogger logger,
            ICacheProvider cacheProvider,
            IDependencyManager dependencyManager,
            IDocumentCollectionNameResolver collectionNameResolver,
            IMongoStorageClientFactory clientFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider;
            _dependencyManager = dependencyManager;
            _collectionNameResolver = collectionNameResolver ?? throw new ArgumentNullException(nameof(collectionNameResolver));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            MongoBsonSerialization.Configure();
        }

        public async Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            Validate(item);
            Prepare(item);
            try
            {
                await GetCollection<TEntity>().InsertOneAsync(item).ConfigureAwait(false);
                if (_cacheProvider != null) await _cacheProvider.AddAsync(GetCacheKey<TEntity>(item.Id), JsonConvert.SerializeObject(item)).ConfigureAwait(false);
                return new OperationResponse<TEntity>(item);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new ContentModifiedException { EntityType = typeof(TEntity).Name, Id = item.Id };
            }
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

            await GetCollection<TEntity>().ReplaceOneAsync(entity => entity.Id == item.Id, item, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
            if (_cacheProvider != null) await _cacheProvider.AddAsync(GetCacheKey<TEntity>(item.Id), JsonConvert.SerializeObject(item)).ConfigureAwait(false);
            return new OperationResponse<TEntity>(item);
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

            var entityFromStore = await GetCollection<TEntity>().Find(entity => entity.Id == id && entity.EntityType == typeof(TEntity).Name).FirstOrDefaultAsync().ConfigureAwait(false);
            if (entityFromStore == null)
            {
                if (throwOnNotFound) throw new RecordNotFoundException(typeof(TEntity).Name, id);
                return null;
            }
            if (_cacheProvider != null) await _cacheProvider.AddAsync(GetCacheKey<TEntity>(id), JsonConvert.SerializeObject(entityFromStore)).ConfigureAwait(false);
            return entityFromStore;
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
            var document = await GetDocumentAsync<TEntity>(id, true).ConfigureAwait(false);
            if (_dependencyManager != null)
            {
                var dependencies = await _dependencyManager.CheckForDependenciesAsync(document).ConfigureAwait(false);
                if (dependencies.IsInUse) throw new InUseException(dependencies);
            }
            if (_cacheProvider != null) await _cacheProvider.RemoveAsync(GetCacheKey<TEntity>(id)).ConfigureAwait(false);
            var result = await GetCollection<TEntity>().DeleteOneAsync(entity => entity.Id == id).ConfigureAwait(false);
            if (!result.IsAcknowledged || result.DeletedCount == 0) throw new RecordNotFoundException(typeof(TEntity).Name, id);
            return new OperationResponse<TEntity>(document);
        }

        public async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return await GetCollection<TEntity>().Find(query).ToListAsync().ConfigureAwait(false);
        }

        public async Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));
            try
            {
                var items = await GetCollection<TEntity>().Find(query)
                    .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                    .Limit(listRequest.PageSize)
                    .ToListAsync().ConfigureAwait(false);
                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                _logger.AddException(nameof(MongoDocumentStorageClient), ex);
                var response = ListResponse<TEntity>.Create(new List<TEntity>());
                response.Errors.Add(new ErrorMessage(ex.Message));
                return response;
            }
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
                    var count = await collection.CountDocumentsAsync(new BsonDocument
                    {
                        { "EntityType", request.GetRequired<string>("entityType") },
                        { "OwnerOrganization.Id", request.GetRequired<string>("orgId") }
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return new[] { (TResult)(object)new DocumentCountResult { Count = checked((int)count) } };

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

        private IMongoCollection<TEntity> GetCollection<TEntity>() where TEntity : class
        {
            var collectionName = _collectionNameResolver.Resolve(_settings.DatabaseName, typeof(TEntity), null);
            return _clientFactory.GetDatabase(_settings.ConnectionString, _settings.DatabaseName).GetCollection<TEntity>(collectionName);
        }

        private IMongoCollection<BsonDocument> GetBsonCollection(string collectionName) =>
            _clientFactory.GetDatabase(_settings.ConnectionString, _settings.DatabaseName).GetCollection<BsonDocument>(collectionName);

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
            documents.Select(BsonSerializer.Deserialize<TResult>).ToList();
    }
}
