using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class StorageUtils
    {
        private readonly Uri _endpoint;
        private readonly string _sharedKey;
        private readonly string _dbName;
        private readonly IAdminLogger _logger;

        private DocumentClient _documentClient;

        public StorageUtils(Uri endpoint, String sharedKey, String dbName, IAdminLogger logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _sharedKey = sharedKey ?? throw new ArgumentNullException(nameof(sharedKey));
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected DocumentClient GetDocumentClient()
        {
            if (_documentClient == null)
            {
                var connectionPolicy = new ConnectionPolicy();
                connectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 10;
                connectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 60;

                _documentClient = new DocumentClient(_endpoint, _sharedKey, connectionPolicy);
            }

            return _documentClient;
        }

        protected async Task<Database> GetDatabase(DocumentClient client)
        {
            if (String.IsNullOrEmpty(_dbName))
            {
                var ex = new InvalidOperationException($"Invalid or missing database name information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            var databases = client.CreateDatabaseQuery().Where(db => db.Id == _dbName).ToArray();
            if (databases.Any())
            {
                return databases.First();
            }

            return await client.CreateDatabaseAsync(new Database() { Id = _dbName });
        }

        private async Task<DocumentCollection> GetCollectionAsync()
        {
            var client = GetDocumentClient();

            var collectionName = _dbName + "_Collections";

            var databases = client.CreateDocumentCollectionQuery((await GetDatabase(GetDocumentClient())).SelfLink).Where(db => db.Id == collectionName).ToArray();
            if (databases.Any())
            {
                return databases.First();
            }

            return await client.CreateDocumentCollectionAsync((await GetDatabase(GetDocumentClient())).SelfLink, new DocumentCollection() { Id = collectionName });
        }

        private String _selfLink;
        protected async Task<String> GetCollectionDocumentsLinkAsync()
        {
            if (String.IsNullOrEmpty(_selfLink))
            {
                _selfLink = (await GetCollectionAsync()).DocumentsLink;
            }

            return _selfLink;
        }

        public async Task<TEntity> FindWithKeyAsync<TEntity>(string key, IEntityHeader org, bool throwOnNotFound = true) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var documentLink = await GetCollectionDocumentsLinkAsync();
            var docClient = GetDocumentClient();

            var docQuery = docClient.CreateDocumentQuery<TEntity>(documentLink)
                .Where(itm => itm.Key == key && itm.OwnerOrganization.Id == org.Id && itm.EntityType == typeof(TEntity).Name)
                .AsDocumentQuery();

            var result = await docQuery.ExecuteNextAsync<TEntity>();
            if (result == null && throwOnNotFound)
            {
                throw new Exception("Null Response from Query");
            }

            return result.FirstOrDefault();
        }

        public async Task<TEntity> FindWithKeyAsync<TEntity>(string key) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var documentLink = await GetCollectionDocumentsLinkAsync();
            var docClient = GetDocumentClient();

            var docQuery = docClient.CreateDocumentQuery<TEntity>(documentLink)
                .Where(itm => itm.Key == key && itm.IsPublic == true && itm.EntityType == typeof(TEntity).Name)
                .AsDocumentQuery();

            var result = await docQuery.ExecuteNextAsync<TEntity>();
            if (result == null)
            {
                throw new Exception("Null Response from Query");
            }

            return result.FirstOrDefault();
        }

        public async Task DeleteIfExistsAsync<TEntity>(string key, IEntityHeader org) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var entity = await FindWithKeyAsync<TEntity>(key, org, false);
            if(entity != null)
            {
                await DeleteAsync<TEntity>(entity.Id, org);
            }
        }


        public async Task DeleteAsync<TEntity>(string id, IEntityHeader org) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var documentLink = await GetCollectionDocumentsLinkAsync();
            var docClient = GetDocumentClient();

            var collectionName = _dbName + "_Collections";
            var docUri = UriFactory.CreateDocumentUri(_dbName, collectionName, id);
            var response =  await docClient.ReadDocumentAsync(docUri);

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

                await docClient.DeleteDocumentAsync(docUri);
            }
        }
    }
}
