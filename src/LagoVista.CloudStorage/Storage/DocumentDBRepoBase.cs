using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using LagoVista.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LagoVista.CloudStorage.Exceptions;
using System.Diagnostics;

namespace LagoVista.CloudStorage.DocumentDB
{
    public class DocumentDBRepoBase<TEntity> : IDisposable where TEntity : class, IIDEntity, INoSQLEntity
    {
        private Uri _endpoint;
        private string _sharedKey;
        private string _dbName;
        private string _collectionName;
        private DocumentClient _documentClient;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;

        public DocumentDBRepoBase(Uri endpoint, String sharedKey, String dbName, IAdminLogger logger, ICacheProvider cacheProvider = null)
        {
            _endpoint = endpoint;
            _sharedKey = sharedKey;
            _dbName = dbName;
            _logger = logger;
            _cacheProvider = cacheProvider;

            _collectionName = typeof(TEntity).Name;
            if (!_collectionName.ToLower().EndsWith("s"))
            {
                _collectionName += "s";
            }
        }

        public DocumentDBRepoBase(string endpoint, String sharedKey, String dbName, IAdminLogger logger, ICacheProvider cacheProvider = null) : this(new Uri(endpoint), sharedKey, dbName, logger, cacheProvider)
        {

        }

        public DocumentDBRepoBase(IAdminLogger logger)
        {
            _logger = logger;

        }

        public void SetConnection(String endPoint, string sharedKey, string dbName)
        {
            var endpoint = endPoint;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _endpoint))
            {
                var ex = new InvalidOperationException($"Invalid or missing end point information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            _sharedKey = sharedKey;
            if (String.IsNullOrEmpty(_sharedKey))
            {
                var ex = new InvalidOperationException($"Invalid or missing shared key information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            _dbName = dbName;
            if (String.IsNullOrEmpty(_dbName))
            {
                var ex = new InvalidOperationException($"Invalid or missing database name information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            _collectionName = typeof(TEntity).Name;
            if (!_collectionName.ToLower().EndsWith("s"))
            {
                _collectionName += "s";
            }
        }

        public async Task DeleteCollectionAsync()
        {
            var client = GetDocumentClient();
            var database = await GetDatabase(client);

            await client.DeleteDatabaseAsync(database.SelfLink);
        }

        protected DocumentClient GetDocumentClient()
        {
            if (_endpoint == null)
            {
                var ex = new InvalidOperationException($"Invalid or missing end point information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            if (String.IsNullOrEmpty(_sharedKey))
            {
                var ex = new InvalidOperationException($"Invalid or missing shared key information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            if (_documentClient == null)
            {
                var connectionPolicy = new ConnectionPolicy();
                connectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 10;
                connectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 60;

                _documentClient = new DocumentClient(_endpoint, _sharedKey, connectionPolicy);
            }

            return _documentClient;
        }

        protected virtual bool ShouldConsolidateCollections
        {
            get { return false; }
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

        protected virtual String GetCollectionName()
        {
            if (ShouldConsolidateCollections)
            {
                if (IsRuntimeData)
                {
                    return _dbName + "_CollectionsRunTime";
                }
                else
                    return _dbName + "_Collections";
            }
            else
            {
                return _collectionName;
            }
        }

        protected virtual bool IsRuntimeData { get { return false; } }


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
            if (item is IValidateable)
            {
                var result = Validator.Validate(item as IValidateable);
                if (!result.Successful)
                {
                    throw new ValidationException("Invalid Data.", result.Errors);
                }
            }

            item.DatabaseName = _dbName;
            item.EntityType = typeof(TEntity).Name;

            var response = await Client.CreateDocumentAsync(await GetCollectionDocumentsLinkAsync(), item);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                _logger.AddCustomEvent(LogLevel.Error, $"DocuementDbRepo<{_dbName}>_CreateDocumentAsync", "Error return code: " + response.StatusCode,
                    new KeyValuePair<string, string>("EntityType", typeof(TEntity).Name),
                    new KeyValuePair<string, string>("Id", item.Id)
                    );
                throw new Exception("Could not insert entity");
            }

            if(_cacheProvider != null)
            {
                await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item));
            }

            return response;
        }

        private string GetCacheKey(string id)
        {
            return $"{_dbName}-{typeof(TEntity).Name}-{id}".ToLower();
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


            item.DatabaseName = _dbName;
            item.EntityType = typeof(TEntity).Name;

            var upsertResult =  await Client.UpsertDocumentAsync(await GetCollectionDocumentsLinkAsync(), item);
            switch(upsertResult.StatusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    _logger.AddError("DocumentDBRepoBase_UpsertDocumentAsync", "BadRequest", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new Exception($"Bad Request on Upsert {typeof(TEntity).Name}");
                case System.Net.HttpStatusCode.Forbidden:
                    _logger.AddError("DocumentDBRepoBase_UpsertDocumentAsync", "Forbidden", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new Exception($"Forbidden on Upsert {typeof(TEntity).Name}");
                case System.Net.HttpStatusCode.Conflict:
                    _logger.AddError("DocumentDBRepoBase_UpsertDocumentAsync", "Conflict", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new ContentModifiedException()
                    {
                        EntityType = typeof(TEntity).Name,
                        Id = item.Id
                    };

                case System.Net.HttpStatusCode.RequestEntityTooLarge:
                    _logger.AddError("DocumentDBRepoBase_UpsertDocumentAsync", "RequestEntityTooLarge", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new Exception($"RequestEntityTooLarge Upsert on type {typeof(TEntity).Name}");
            }

            if(_cacheProvider != null)
            {
                await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item));
            }

            return upsertResult;
        }

        protected async Task<TEntity> GetDocumentAsync(string id,  bool throwOnNotFound = true)
        {
            if (_cacheProvider != null)
            {
                var json = await _cacheProvider.GetAsync(GetCacheKey(id));
                if (!String.IsNullOrEmpty(json))
                {
                    var entity = JsonConvert.DeserializeObject<TEntity>(json);
                    if (entity.EntityType != typeof(TEntity).Name)
                    {
                        if (throwOnNotFound)
                        {
                            _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Type Mismatch", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Actual Type", entity.EntityType), new KeyValuePair<string, string>("id", id));
                            throw new RecordNotFoundException(typeof(TEntity).Name, id);
                        }
                        else
                        {
                            return default;
                        }
                    }
                    else
                        return entity;
                }
            }

            var doc = await GetDocumentAsync(id, null, throwOnNotFound);
            if(_cacheProvider != null)
            {
                await _cacheProvider.AddAsync(GetCacheKey(id), JsonConvert.SerializeObject(doc));
            }

            return doc;
        }
    
        protected async Task<TEntity> GetDocumentAsync(string id, string partitionKey, bool throwOnNotFound = true)
        {
            try
            {

                var docUri = UriFactory.CreateDocumentUri(_dbName, GetCollectionName(), id);
                var sw = Stopwatch.StartNew();
                //We have the Id as Id (case sensitive) so we can work with C# naming conventions, if we use Linq it uses the in Id rather than the "id" that DocumentDB requires.
                var response = String.IsNullOrEmpty(partitionKey) ? 
                    await Client.ReadDocumentAsync(docUri) : 
                    await Client.ReadDocumentAsync(docUri, new RequestOptions() { PartitionKey = new PartitionKey(partitionKey) });
                Console.WriteLine($"sw=> Get document {typeof(TEntity).Name} in {sw.Elapsed.TotalMilliseconds}ms, Resource Charge: {response.RequestCharge}");

                if (response == null)
                {
                    _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Empty Response", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("id", id));
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = response.Resource.ToString();

                    if (String.IsNullOrEmpty(json))
                    {
                        _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Empty Response Content", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("id", id));
                        throw new RecordNotFoundException(typeof(TEntity).Name, id);
                    }

                    var entity = JsonConvert.DeserializeObject<TEntity>(json);
                    if (entity.EntityType != typeof(TEntity).Name)
                    {
                        if (throwOnNotFound)
                        {
                            _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Type Mismatch", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Actual Type", entity.EntityType), new KeyValuePair<string, string>("id", id));
                            throw new RecordNotFoundException(typeof(TEntity).Name, id);
                        }
                        else
                        {
                            return default;
                        }
                    }



                    return entity;
                }
                else
                {
                    if (throwOnNotFound)
                    {
                        _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Error requesting document", new KeyValuePair<string, string>("Invalid Status Code", response.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                        throw new RecordNotFoundException(typeof(TEntity).Name, id);
                    }
                    else
                    {
                        return default;
                    }
                }
            }
            catch (DocumentClientException ex)
            {
                _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Error requesting document", new KeyValuePair<string, string>("DocumentClientException", ex.Message), new KeyValuePair<string, string>("StatusCode", ex.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                if (throwOnNotFound)
                {
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();

                _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Error requesting document", new KeyValuePair<string, string>("Exception", ex.Message), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                if (throwOnNotFound)
                {
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }
                else
                {
                    return null;
                }
            }
        }

        protected async Task<ResourceResponse<Document>> DeleteDocumentAsync(string id)
        {
            var docUri = UriFactory.CreateDocumentUri(_dbName, GetCollectionName(), id);
            if(_cacheProvider != null)
            {
                await _cacheProvider.RemoveAsync(GetCacheKey(id));
            }

            return await Client.DeleteDocumentAsync(docUri);
        }

        protected async Task<ResourceResponse<Document>> DeleteDocumentAsync(string id, string partitionKey)
        {
            var docUri = UriFactory.CreateDocumentUri(_dbName, GetCollectionName(), id);
            return await Client.DeleteDocumentAsync(docUri, new RequestOptions() { PartitionKey = new PartitionKey(partitionKey) });
        }

        protected async Task<IEnumerable<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query)
        {
            var documentLink = await GetCollectionDocumentsLinkAsync();
            var sw = Stopwatch.StartNew();
            var docQuery = Client.CreateDocumentQuery<TEntity>(documentLink, new FeedOptions() { MaxItemCount = 9999 });

            var linqQuery = (docQuery).Where(query).Where(itm => itm.EntityType == typeof(TEntity).Name).AsDocumentQuery();

            var result = await linqQuery.ExecuteNextAsync<TEntity>();

            Console.WriteLine($"=> SW => {sw.Elapsed.TotalMilliseconds}, Result: {result.RequestCharge}, {result.RequestDiagnosticsString} {result.ResponseContinuation}");
            return result;
        }

        protected async Task<IEnumerable<TEntity>> QueryAsync(string sql, params SqlParameter[] sqlParams)
        {
            var query = new SqlQuerySpec(sql, new SqlParameterCollection(sqlParams ));
            var sw = Stopwatch.StartNew();

            var documentLink = await GetCollectionDocumentsLinkAsync();
            var docQuery =  Client.CreateDocumentQuery<TEntity>(documentLink, query, new FeedOptions() { MaxItemCount = 9999 }).AsDocumentQuery();
            var result = await docQuery.ExecuteNextAsync<TEntity>();
            Console.WriteLine($"=> SW => {sw.Elapsed.TotalMilliseconds}, Result: {result.RequestCharge}, {result.RequestDiagnosticsString} {result.ResponseContinuation}");

            return result;
        }

        protected async Task<ListResponse<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            try
            {
                var options = new FeedOptions()
                {
                    MaxItemCount = (listRequest.PageSize == 0) ? 50 : listRequest.PageSize
                };

                if (!String.IsNullOrEmpty(listRequest.NextRowKey))
                {
                    options.RequestContinuation = listRequest.NextRowKey;
                }

                var documentLink = await GetCollectionDocumentsLinkAsync();

                var sw = Stopwatch.StartNew();

                var docQuery = Client.CreateDocumentQuery<TEntity>(documentLink, options)
                    .Where(query).Where(itm => itm.EntityType == typeof(TEntity).Name).AsDocumentQuery();

                var result = await docQuery.ExecuteNextAsync<TEntity>();
                if(result == null)
                {
                    throw new Exception("Null Response from Query");
                }

                var listResponse = ListResponse<TEntity>.Create(result);
                listResponse.NextRowKey = result.ResponseContinuation;
                listResponse.PageSize = result.Count;
                listResponse.HasMoreRecords = result.Count == listRequest.PageSize;
                listResponse.PageIndex = listRequest.PageIndex;
                Console.WriteLine($"sw=> Query execution in {sw.Elapsed.TotalMilliseconds}");


                return listResponse;
            }
            catch(Exception ex)
            {
                _logger.AddException("DocumentDBBase", ex, typeof(TEntity).Name.ToKVP("entityType"));

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));                
                return listResponse;
            }
        }

        /// <summary>
        /// Return all objects, independent of entity type
        /// </summary>
        /// <param name="query"></param>
        /// <param name="listRequest"></param>
        /// <returns></returns>
        protected async Task<ListResponse<TEntity>> QueryAllAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            try
            {
                var options = new FeedOptions()
                {
                    MaxItemCount = (listRequest.PageSize == 0) ? 50 : listRequest.PageSize
                };

                if (!String.IsNullOrEmpty(listRequest.NextRowKey))
                {
                    options.RequestContinuation = listRequest.NextRowKey;
                }

                var sw = Stopwatch.StartNew();

                var documentLink = await GetCollectionDocumentsLinkAsync();

                var docQuery = Client.CreateDocumentQuery<TEntity>(documentLink, options)
                    .Where(query).AsDocumentQuery();

                var result = await docQuery.ExecuteNextAsync<TEntity>();
                if (result == null)
                {
                    throw new Exception("Null Response from Query");
                }


                var listResponse = ListResponse<TEntity>.Create(result);
                listResponse.NextRowKey = result.ResponseContinuation;
                listResponse.PageSize = result.Count;
                listResponse.HasMoreRecords = result.Count == listRequest.PageSize;
                listResponse.PageIndex = listRequest.PageIndex;
                Console.WriteLine($"sw=> Query execution in {sw.Elapsed.TotalMilliseconds}");

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException("DocumentDBBase", ex, typeof(TEntity).Name.ToKVP("entityType"));

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        protected async Task<ListResponse<TEntity>> DescOrderQueryAsync<TKey>(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, System.Linq.Expressions.Expression<Func<TEntity, TKey>> orderBy, ListRequest listRequest)
        {
            try
            {
                var options = new FeedOptions()
                {
                    MaxItemCount = (listRequest.PageSize == 0) ? 50 : listRequest.PageSize
                };

                if (!String.IsNullOrEmpty(listRequest.NextRowKey))
                {
                    options.RequestContinuation = listRequest.NextRowKey;
                }

                var sw = Stopwatch.StartNew();
                var documentLink = await GetCollectionDocumentsLinkAsync();

                var docQuery = Client.CreateDocumentQuery<TEntity>(documentLink, options)
                    .Where(query).Where(itm => itm.EntityType == typeof(TEntity).Name).OrderByDescending(orderBy).AsDocumentQuery();

                var result = await docQuery.ExecuteNextAsync<TEntity>();
                if (result == null)
                {
                    throw new Exception("Null Response from Query");
                }

                var listResponse = ListResponse<TEntity>.Create(result);
                listResponse.NextRowKey = result.ResponseContinuation;
                listResponse.PageSize = result.Count;
                listResponse.HasMoreRecords = result.Count == listRequest.PageSize;
                listResponse.PageIndex = listRequest.PageIndex;
                Console.WriteLine($"sw=> Query execution in {sw.Elapsed.TotalMilliseconds}");

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException("DocumentDBBase", ex, typeof(TEntity).Name.ToKVP("entityType"));

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        protected async Task<IEnumerable<TEntity>> QueryAsync(string query, SqlParameterCollection sqlParams)
        {
            var spec = new SqlQuerySpec(query, sqlParams);
            return Client.CreateDocumentQuery<TEntity>(await GetCollectionDocumentsLinkAsync(), spec);
        }

        public void Dispose()
        {

        }
    }
}
