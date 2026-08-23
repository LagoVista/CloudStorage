// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 3d64b2c5a6246e48e6d8b58d8f05580b11b5538f85a70b83a071b1d446265b65
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    internal class MongoDBStorage<TEntity> : IDocumentDBRepoBase<TEntity> where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
    {
        private string _connectionString;
        private string _dbName;
        private MongoClient _mongoClient;
        private IMongoDatabase _mongoDb;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IDependencyManager _dependencyManager;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;

        public MongoDBStorage(string connectionString, string dbName, IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null, IDocumentCollectionNameResolver collectionNameResolver = null)
        {
            _logger = logger;
            _cacheProvider = cacheProvider;
            _dependencyManager = dependencyManager;
            _collectionNameResolver = collectionNameResolver ?? new DocumentCollectionNameResolver();
            SetConnection(connectionString, null, dbName);
        }

        public async Task<OperationResponse<TEntity>> CreateDocumentAsync(TEntity item)
        {
            ValidateEntity(item);
            PrepareEntity(item);
            await GetCollection<TEntity>().InsertOneAsync(item).ConfigureAwait(false);
            await AddToCacheAsync(item).ConfigureAwait(false);
            return new OperationResponse<TEntity>(item);
        }

        public async Task DeleteCollectionAsync()
        {
            await GetDatabase().DropCollectionAsync(GetCollectionName()).ConfigureAwait(false);
        }

        public async Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id)
        {
            return await DeleteDocumentAsync(id, null).ConfigureAwait(false);
        }

        public async Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id, string partitionKey)
        {
            var existing = await GetDocumentAsync(id).ConfigureAwait(false);
            if (_dependencyManager != null)
            {
                var dependencies = await _dependencyManager.CheckForDependenciesAsync(existing).ConfigureAwait(false);
                if (dependencies.IsInUse) throw new InUseException(dependencies);
            }

            await RemoveFromCacheAsync(id).ConfigureAwait(false);
            var result = await GetCollection<TEntity>().DeleteOneAsync(Builders<TEntity>.Filter.Eq(entity => entity.Id, id)).ConfigureAwait(false);
            if (result.DeletedCount == 0) throw new RecordNotFoundException(typeof(TEntity).Name, id);
            return new OperationResponse<TEntity>(existing);
        }

        public async Task<ListResponse<TEntity>> DescOrderQueryAsync<TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> orderBy, ListRequest listRequest)
        {
            try
            {
                var items = await GetCollection<TEntity>().Find(CreateEntityFilter(query)).Sort(Builders<TEntity>.Sort.Descending(orderBy)).Skip(GetSkip(listRequest)).Limit(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<TEntity>(ex, listRequest);
            }
        }

        public string GetCollectionName()
        {
            return _collectionNameResolver.Resolve(_dbName, typeof(TEntity));
        }

        public async Task<TEntity> GetDocumentAsync(string id, bool throwOnNotFound = true)
        {
            if (_cacheProvider != null)
            {
                var cached = await _cacheProvider.GetAsync(GetCacheKey(id)).ConfigureAwait(false);
                if (!String.IsNullOrWhiteSpace(cached))
                {
                    try
                    {
                        var cachedEntity = JsonConvert.DeserializeObject<TEntity>(cached);
                        if (cachedEntity != null && String.Equals(cachedEntity.EntityType, typeof(TEntity).Name, StringComparison.Ordinal)) return cachedEntity;
                        await _cacheProvider.RemoveAsync(GetCacheKey(id)).ConfigureAwait(false);
                    }
                    catch
                    {
                        await _cacheProvider.RemoveAsync(GetCacheKey(id)).ConfigureAwait(false);
                    }
                }
            }

            var entity = await GetDocumentAsync(id, null, throwOnNotFound).ConfigureAwait(false);
            if (entity != null) await AddToCacheAsync(entity).ConfigureAwait(false);
            return entity;
        }

        public async Task<TEntity> GetDocumentAsync(string id, string partitionKey, bool throwOnNotFound = true)
        {
            try
            {
                var filter = Builders<TEntity>.Filter.Eq(entity => entity.Id, id) & Builders<TEntity>.Filter.Eq(entity => entity.EntityType, typeof(TEntity).Name);
                var entity = await GetCollection<TEntity>().Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
                if (entity != null) return entity;
                if (throwOnNotFound) throw new RecordNotFoundException(typeof(TEntity).Name, id);
                return null;
            }
            catch (RecordNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.AddException("MongoDBStorage_GetDocumentAsync", ex);
                if (throwOnNotFound) throw new RecordNotFoundException(typeof(TEntity).Name, id);
                return null;
            }
        }

        public string GetPartitionKey()
        {
            return null;
        }

        public async Task<ListResponse<TEntity>> QueryAllAsync(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            return await QueryAsync(query, listRequest).ConfigureAwait(false);
        }

        public async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> query)
        {
            return await GetCollection<TEntity>().Find(CreateEntityFilter(query)).ToListAsync().ConfigureAwait(false);
        }

        public async Task<ListResponse<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            try
            {
                var items = await GetCollection<TEntity>().Find(CreateEntityFilter(query)).Skip(GetSkip(listRequest)).Limit(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<TEntity>(ex, listRequest);
            }
        }

        public async Task<ListResponse<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest)
        {
            try
            {
                var find = GetCollection<TEntity>().Find(CreateEntityFilter(query));
                if (sort != null) find = find.Sort(Builders<TEntity>.Sort.Ascending(sort));
                var items = await find.Skip(GetSkip(listRequest)).Limit(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<TEntity>(ex, listRequest);
            }
        }

        public async Task<ListResponse<TEntity>> QueryDescendingAsync(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest)
        {
            try
            {
                var find = GetCollection<TEntity>().Find(CreateEntityFilter(query));
                if (sort != null) find = find.Sort(Builders<TEntity>.Sort.Descending(sort));
                var items = await find.Skip(GetSkip(listRequest)).Limit(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<TEntity>(ex, listRequest);
            }
        }

        public async Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary, TEntityFactory>(Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory : class, ISummaryFactory, INoSQLEntity
        {
            try
            {
                var find = GetCollection<TEntityFactory>().Find(CreateFactoryFilter(query));
                if (sort != null) find = find.Sort(Builders<TEntityFactory>.Sort.Ascending(sort));
                var items = await find.Skip(GetSkip(listRequest)).Limit(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
                return ListResponse<TEntitySummary>.Create(listRequest, items.Select(item => item.CreateSummary() as TEntitySummary));
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<TEntitySummary>(ex, listRequest);
            }
        }

        public async Task<ListResponse<TEntitySummary>> QuerySummaryDescendingAsync<TEntitySummary, TEntityFactory>(Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory : class, ISummaryFactory, INoSQLEntity
        {
            try
            {
                var find = GetCollection<TEntityFactory>().Find(CreateFactoryFilter(query));
                if (sort != null) find = find.Sort(Builders<TEntityFactory>.Sort.Descending(sort));
                var items = await find.Skip(GetSkip(listRequest)).Limit(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
                return ListResponse<TEntitySummary>.Create(listRequest, items.Select(item => item.CreateSummary() as TEntitySummary));
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<TEntitySummary>(ex, listRequest);
            }
        }

        public void SetConnection(string connectionString, string sharedKey, string dbName)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException($"Invalid or missing Mongo connection string on {GetType().Name}");
            if (String.IsNullOrWhiteSpace(dbName)) throw new InvalidOperationException($"Invalid or missing database name on {GetType().Name}");
            _connectionString = connectionString;
            _dbName = dbName;
            _mongoClient = new MongoClient(_connectionString);
            _mongoDb = _mongoClient.GetDatabase(_dbName);
        }

        public async Task<OperationResponse<TEntity>> UpsertDocumentAsync(TEntity item)
        {
            ValidateEntity(item);
            PrepareEntity(item);

            if (_dependencyManager != null)
            {
                var existing = await GetDocumentAsync(item.Id, false).ConfigureAwait(false);
                if (existing != null && !String.Equals(existing.Name, item.Name, StringComparison.Ordinal))
                {
                    var dependencyResult = await _dependencyManager.CheckForDependenciesAsync(item).ConfigureAwait(false);
                    if (dependencyResult.IsInUse)
                    {
                        foreach (var dependentObject in dependencyResult.DependentObjects) await _dependencyManager.RenameDependentObjectsAsync(item.LastUpdatedBy, item.Id, item.GetType().Name, dependentObject.Id, dependentObject.RecordType, item.Name).ConfigureAwait(false);
                    }
                    await _dependencyManager.RenameObjectAsync(item.LastUpdatedBy, item.Id, item.GetType().Name, item.Name).ConfigureAwait(false);
                }
            }

            var result = await GetCollection<TEntity>().ReplaceOneAsync(Builders<TEntity>.Filter.Eq(entity => entity.Id, item.Id), item, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
            if (!result.IsAcknowledged) throw new InvalidOperationException($"Mongo did not acknowledge upsert for {typeof(TEntity).Name} {item.Id}.");
            await AddToCacheAsync(item).ConfigureAwait(false);
            return new OperationResponse<TEntity>(item);
        }

        private async Task AddToCacheAsync(TEntity item)
        {
            if (_cacheProvider != null) await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item)).ConfigureAwait(false);
        }

        private FilterDefinition<TEntity> CreateEntityFilter(Expression<Func<TEntity, bool>> query)
        {
            var entityTypeFilter = Builders<TEntity>.Filter.Eq(entity => entity.EntityType, typeof(TEntity).Name);
            return query == null ? entityTypeFilter : Builders<TEntity>.Filter.Where(query) & entityTypeFilter;
        }

        private FilterDefinition<TEntityFactory> CreateFactoryFilter<TEntityFactory>(Expression<Func<TEntityFactory, bool>> query) where TEntityFactory : class, ISummaryFactory, INoSQLEntity
        {
            var entityTypeFilter = Builders<TEntityFactory>.Filter.Eq("EntityType", typeof(TEntity).Name);
            return query == null ? entityTypeFilter : Builders<TEntityFactory>.Filter.Where(query) & entityTypeFilter;
        }

        private ListResponse<TItem> CreateErrorResponse<TItem>(Exception ex, ListRequest listRequest)
        {
            _logger?.AddException("MongoDBStorage_Query", ex);
            var response = ListResponse<TItem>.Create(new List<TItem>());
            response.Errors.Add(new ErrorMessage(ex.Message));
            return response;
        }

        private string GetCacheKey(string id)
        {
            return $"{_dbName}-{typeof(TEntity).Name}-{id}".ToLowerInvariant();
        }

        private IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class
        {
            return GetDatabase().GetCollection<TDocument>(GetCollectionName());
        }

        private IMongoDatabase GetDatabase()
        {
            if (_mongoDb == null) throw new InvalidOperationException("Mongo database connection has not been initialized.");
            return _mongoDb;
        }

        private static int GetSkip(ListRequest listRequest)
        {
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));
            return Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize;
        }

        private async Task RemoveFromCacheAsync(string id)
        {
            if (_cacheProvider != null) await _cacheProvider.RemoveAsync(GetCacheKey(id)).ConfigureAwait(false);
        }

        private void PrepareEntity(TEntity item)
        {
            item.DatabaseName = _dbName;
            item.EntityType = typeof(TEntity).Name;
        }

        private static void ValidateEntity(TEntity item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item is IValidateable validateable)
            {
                var result = Validator.Validate(validateable);
                if (!result.Successful) throw new ValidationException("Invalid Data.", result.Errors);
            }
        }
    }
}
