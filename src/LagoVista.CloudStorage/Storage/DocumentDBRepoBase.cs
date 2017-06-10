using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.DocumentDB
{
    public class DocumentDBRepoBase<TEntity> : IDisposable where TEntity : class, IIDEntity, INoSQLEntity
    {
        private Uri _endpoint;
        private string _sharedKey;
        private string _dbName;
        private string _collectionName;
        private DocumentClient _documentClient;
        private IAdminLogger _logger;

        public DocumentDBRepoBase(Uri endpoint, String sharedKey, String dbName, IAdminLogger logger)
        {
            _endpoint = endpoint;
            _sharedKey = sharedKey;
            _dbName = dbName;
            _logger = logger;

            _collectionName = typeof(TEntity).Name;
            if (!_collectionName.ToLower().EndsWith("s"))
            {
                _collectionName += "s";
            }
        }

        public DocumentDBRepoBase(string endpoint, String sharedKey, String dbName, IAdminLogger logger) : this(new Uri(endpoint), sharedKey, dbName, logger)
        {

        }

        public async Task DeleteCollectionAsync()
        {
            var client = GetDocumentClient();
            var database = await GetDatabase(client);

            await client.DeleteDatabaseAsync(database.SelfLink);
        }

        protected DocumentClient GetDocumentClient()
        {
            if (_documentClient == null)
            {
                _documentClient = new DocumentClient(_endpoint, _sharedKey);
            }

            return _documentClient;
        }

        protected virtual bool ShouldConsolidateCollections
        {
            get { return false; }
        }

        protected async Task<Database> GetDatabase(DocumentClient client)
        {
            var databases = client.CreateDatabaseQuery().Where(db => db.Id == _dbName).ToArray();
            if (databases.Any())
            {
                return databases.First();
            }

            return await client.CreateDatabaseAsync(new Database() { Id = _dbName });
        }

        private String GetCollectionName()
        {
            return ShouldConsolidateCollections? _dbName +"_Collections" : _collectionName;
        }

        public async Task<DocumentCollection> GetCollectionAsync()
        {
            var client = GetDocumentClient();

            var databases = client.CreateDocumentCollectionQuery((await GetDatabase(GetDocumentClient())).SelfLink).Where(db => db.Id == GetCollectionName()).ToArray();
            if (databases.Any())
            {
                return databases.First();
            }

            return await client.CreateDocumentCollectionAsync((await GetDatabase(GetDocumentClient())).SelfLink, new DocumentCollection() { Id = GetCollectionName() });
        }

        protected DocumentClient Client
        {
            get { return GetDocumentClient(); }
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

        protected async Task<ResourceResponse<Document>> CreateDocumentAsync(TEntity item)
        {
            if(item is IValidateable)
            {
                var result = Validator.Validate(item as IValidateable);
                if(!result.Successful)
                {
                    throw new ValidationException("Invalid Data.", result.Errors);
                }                    
            }

            item.DatabaseName = _dbName;
            item.EntityType = typeof(TEntity).Name;

            var response = await Client.CreateDocumentAsync(await GetCollectionDocumentsLinkAsync(), item);
            if(response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                _logger.AddCustomEvent(LogLevel.Error, $"DocuementDbRepo<{_dbName}>_CreateDocumentAsync", "Error return code: " + response.StatusCode, 
                    new KeyValuePair<string, string>("EntityType", typeof(TEntity).Name),
                    new KeyValuePair<string, string>("Id", item.Id)
                    );
                throw new Exception("Could not insert entity");
            }
            return response;
        }

        protected async Task<ResourceResponse<Document>> UpsertDocumentAsync(TEntity item)
        {
            if (item is IValidateable)
            {
                var result = Validator.Validate(item as IValidateable);
                if (!result.Successful)
                {
                    throw new ValidationException("Invalid Data.", result.Errors);
                }
            }

            return await Client.UpsertDocumentAsync(await GetCollectionDocumentsLinkAsync(), item);
        }

        protected async Task<TEntity> GetDocumentAsync(string id)
        {
            bool retry = false;
            do
            {
                try
                {
                    //We have the Id as Id (case sensitive) so we can work with C# naming conventions, if we use Linq it uses the in Id rather than the "id" that DocumentDB requires.
                    var response = await Client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_dbName, GetCollectionName(), id));
                    if (response == null)
                    {
                        _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Empty Response", new KeyValuePair<string, string>("EntityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                        throw new RecordNotFoundException(typeof(TEntity).Name, id);
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = response.Resource.ToString();

                        if (String.IsNullOrEmpty(json))
                        {
                            _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Empty Response Content", new KeyValuePair<string, string>("EntityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                            throw new RecordNotFoundException(typeof(TEntity).Name, id);
                        }

                        var entity = JsonConvert.DeserializeObject<TEntity>(json);
                        if (entity.EntityType != typeof(TEntity).Name)
                        {
                            _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Type Mismatch", new KeyValuePair<string, string>("EntityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Actual Type", entity.EntityType), new KeyValuePair<string, string>("Id", id));
                            throw new RecordNotFoundException(typeof(TEntity).Name, id);
                        }

                        return entity;
                    }
                    else
                    {
                        _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Error requesting document", new KeyValuePair<string, string>("Invalid Status Code", response.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                        throw new RecordNotFoundException(typeof(TEntity).Name, id);
                    }
                }
                catch (DocumentClientException ex)
                {                    
                    _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Error requesting document", new KeyValuePair<string, string>("DocumentClientException", ex.Message), new KeyValuePair<string, string>("StatusCode", ex.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }
                catch (Exception ex)
                {
                    _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Error requesting document", new KeyValuePair<string, string>("Exception", ex.Message), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }
            }
            while (retry);
        }

        protected async Task<ResourceResponse<Document>> DeleteDocumentAsync(string id)
        {
            var docUri = UriFactory.CreateDocumentUri(_dbName, GetCollectionName(), id);
            return await Client.DeleteDocumentAsync(docUri);
        }

        private async Task<IOrderedQueryable<TEntity>> GetQueryAsync()
        {
            return Client.CreateDocumentQuery<TEntity>(await GetCollectionDocumentsLinkAsync());
        }

        protected async Task<IEnumerable<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query)
        {
            return (await GetQueryAsync()).Where(query).Where(itm=>itm.EntityType == typeof(TEntity).Name);
        }

        public void Dispose()
        {

        }
    }
}
