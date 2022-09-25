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
    
    public class StorageUtils
    {
        private readonly string _connectionString;
        private readonly string _sharedKey;
        private readonly string _dbName;
        private readonly IAdminLogger _logger;
        private string _collectionName;
        private CosmosClient _client;
        private readonly ICacheProvider _cacheProvider;


        public StorageUtils(Uri endpoint, String sharedKey, String dbName, IAdminLogger logger, ICacheProvider cacheProvider = null)
        {
            _sharedKey = sharedKey ?? throw new ArgumentNullException(nameof(sharedKey));
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider;

            _connectionString = "";
        }

        protected CosmosClient GetDocumentClient()
        {

            if (_client == null)
            {
                var connectionPolicy = new CosmosClientOptions();
                _client = new CosmosClient(_connectionString, _sharedKey, connectionPolicy);
            }
            return _client;
        }

        protected CosmosClient Client
        {
            get { return GetDocumentClient(); }
        }


        protected async Task<Database> GetDatabase(CosmosClient client)
        {
            if (String.IsNullOrEmpty(_dbName))
            {
                var ex = new InvalidOperationException($"Invalid or missing database name information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            return _client.GetDatabase(_dbName);
        }
        public async Task<TEntity> FindWithKeyAsync<TEntity>(string key, IEntityHeader org, bool throwOnNotFound = true) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(doc => doc.Key == key && doc.Id == org.Id && doc.EntityType == typeof(TEntity).Name);

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                if(iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.SingleOrDefault();
                }
            }

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

        public async Task DeleteByKeyIfExistsAsync<TEntity>(string key, IEntityHeader org) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var item = await FindWithKeyAsync<TEntity>(key, org);
            if(item == null)
                await DeleteAsync<TEntity>(item.Id, org);
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

                var entity = JsonConvert.DeserializeObject<TEntity>(json);
                if (org.Id != entity.OwnerOrganization.Id)
                {
                    throw new NotAuthorizedException($"Attempt to delete record of type {typeof(TEntity).Name} owned by org {entity.OwnerOrganization.Text} by org {org.Text}");
                }

                await container.DeleteItemAsync<TEntity>(id, PartitionKey.None);
            }
        }
    }
}
