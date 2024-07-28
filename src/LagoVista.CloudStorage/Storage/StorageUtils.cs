using Amazon.SecurityToken.Internal;
using Azure;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{

    public class StorageUtils : IStorageUtils
    {
        string _endPoint;
        private string _sharedKey;
        private string _dbName;
        private readonly IAdminLogger _logger;
        private string _collectionName;
        private CosmosClient _client;
        private readonly ICacheProvider _cacheProvider;


        public StorageUtils(IAdminLogger logger)
        {
            _logger = logger;
        }


        public StorageUtils(Uri endpoint, String sharedKey, String dbName, IAdminLogger logger, ICacheProvider cacheProvider = null)
        {
            _sharedKey = sharedKey ?? throw new ArgumentNullException(nameof(sharedKey));
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider;
            _collectionName = _dbName + "_Collections";

            _endPoint = endpoint.ToString();
        }

        public void SetConnection(IConnectionSettings connectionSettings)
        {
            _endPoint = connectionSettings.Uri;
            _sharedKey = connectionSettings.AccessKey;
            _dbName = connectionSettings.ResourceName;
            _collectionName = _dbName + "_Collections";
        }

        protected CosmosClient GetDocumentClient()
        {

            if (_client == null)
            {
                var connectionPolicy = new CosmosClientOptions();
                _client = new CosmosClient(_endPoint, _sharedKey, connectionPolicy);
            }
            return _client;
        }

        protected CosmosClient Client
        {
            get { return GetDocumentClient(); }
        }


        protected Task<Database> GetDatabase(CosmosClient client)
        {
            if (String.IsNullOrEmpty(_dbName))
            {
                var ex = new InvalidOperationException($"Invalid or missing database name information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            return Task.FromResult( _client.GetDatabase(_dbName));
        }
        public async Task<TEntity> FindWithKeyAsync<TEntity>(string key, IEntityHeader org, bool throwOnNotFound = true) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(doc => doc.Key == key && doc.OwnerOrganization.Id == org.Id && doc.EntityType == typeof(TEntity).Name);

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Success found {key} of type {typeof(TEntity).Name} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");
                    return response.SingleOrDefault();
                }
            }

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Failed did not find {key} of type {typeof(TEntity).Name} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");

            return null;
        }

        public async Task<IStandardModel> FindWithKeyAsync(string objectType, string key, IEntityHeader org, bool throwOnNotFound = true) 
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<IStandardModel>()
                    .Where(doc => doc.Key == key && doc.OwnerOrganization.Id == org.Id && doc.EntityType ==  objectType);

            using (var iterator = linqQuery.ToFeedIterator<IStandardModel>())
            {
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Success found {key} of type {objectType} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");
                    return response.SingleOrDefault();
                }
            }

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Failed did not find {key} of type {objectType} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");

            return null;
        }

        public async Task<TEntity> FindWithKeyAsync<TEntity>(string key) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(doc => doc.Key == key && doc.IsPublic && doc.EntityType == typeof(TEntity).Name);

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.SingleOrDefault();
                }
            }

            return null;
        }

        public async Task<TEntity> FindWithIdAsync<TEntity>(string id, string ownerId) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);
            var response = await container.ReadItemAsync<TEntity>(id, PartitionKey.None);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var entity = response.Resource;

                if (entity.EntityType != typeof(TEntity).Name)
                {

                    return default;
                }

                if (entity.OwnerOrganization.Id != ownerId)
                    throw new NotAuthorizedException($"Invalid object access by incorrect organization, object org: {entity.OwnerOrganization.Id} - request org: {ownerId}");

                return entity;
            }
            else
            {
                return default;
            }               
        }

        public async Task DeleteByKeyIfExistsAsync<TEntity>(string key, IEntityHeader org) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var item = await FindWithKeyAsync<TEntity>(key, org);
            if (item != null)
                await DeleteAsync<TEntity>(item.Id, org);
        }


        public async Task UpsertDocumentAsync<TEntity>(TEntity obj) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);
            obj.EntityType = typeof(TEntity).Name;
            obj.DatabaseName = _dbName;

            var response = await container.UpsertItemAsync(obj);
            if (response.StatusCode != System.Net.HttpStatusCode.Created) 
            {
                throw new Exception("Could not upsert document.");
            }
        }

        public async Task DeleteAsync<TEntity>(string id, IEntityHeader org) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);

            var response = await container.ReadItemAsync<TEntity>(id, PartitionKey.None);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var json = response.Resource.ToString();

                if (String.IsNullOrEmpty(json))
                {
                    _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Empty Response Content", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("id", id));
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }

                if (org.Id != response.Resource.OwnerOrganization.Id)
                {
                    throw new NotAuthorizedException($"Attempt to delete record of type {typeof(TEntity).Name} owned by org {response.Resource.OwnerOrganization.Text} by org {org.Text}");
                }

                await container.DeleteItemAsync<TEntity>(id, PartitionKey.None);
            }
        }


        public async Task<List<TEntity>> FindByTypeAsync<TEntity>(string entityType, IEntityHeader org) where TEntity : class, IIDEntity, INamedEntity, IOwnedEntity, INoSQLEntity, IKeyedEntity
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(doc => doc.EntityType == entityType && doc.OwnerOrganization.Id == org.Id);

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Query {linqQuery}");

            var entities = new List<TEntity>();

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                if (iterator.HasMoreResults)
                {
                    return (await iterator.ReadNextAsync()).ToList();
                }
            }

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Failed did not find {entityType} of type {typeof(TEntity).Name} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");

            return null;

        }

    }
}
