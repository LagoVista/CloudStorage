using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Newtonsoft.Json;
using System;
using LagoVista.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LagoVista.CloudStorage.Exceptions;
using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Prometheus;

namespace LagoVista.CloudStorage.DocumentDB
{


    public class DocumentDBRepoBase<TEntity> : IDisposable where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
    {
        private string _endPointString;
        private string _sharedKey;
        private string _dbName;
        private string _defaultCollectionName;
        private CosmosClient _cosmosClient;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IDependencyManager _dependencyManager;

        private bool _verboseLogging = false;

        private static bool _isDBCheckComplete = false;

        private static readonly Gauge SQLInsertMetric = Metrics.CreateGauge("sql_insert", "Elapsed time for SQL insert.",
           new GaugeConfiguration
           {
                // Here you specify only the names of the labels.
                LabelNames = new[] { "action" }
           });

        protected static readonly Gauge DocumentRequestCharge = Metrics.CreateGauge("nuviot_document_request_charge", "Elapsed time for document get.","collection");
        protected static readonly Histogram DocumentGet = Metrics.CreateHistogram("nuviot_document_get", "Elapsed time for document get.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });



        protected static readonly Histogram DocumentInsert = Metrics.CreateHistogram("nuviot_document_insert", "Elapsed time for document insert.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });
        
        protected static readonly Histogram DocumentUpdate = Metrics.CreateHistogram("nuviot_document_update", "Elapsed time for document update.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });
        
        protected static readonly Histogram DocumentDelete = Metrics.CreateHistogram("nuviot_document_delete", "Elapsed time for document delete.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram DocumentQuery = Metrics.CreateHistogram("nuviot_document_query", "Elapsed time for document query.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });


        protected static readonly Counter DocumentErrors = Metrics.CreateCounter("nuviot_document_errors", "Error count in document store.", "entity");
        protected static readonly Counter DocumentNotFound = Metrics.CreateCounter("nuviot_document_record_not_found", "Record not found count.", "entity");
        protected static readonly Counter DocumentCacheHit = Metrics.CreateCounter("nuviot_document_cache_hit", "Document Cache Hit.","entity");
        protected static readonly Counter DocumentCacheMiss = Metrics.CreateCounter("nuviot_document_cache_miss", "Document Cache Miss.", "entity");
        protected static readonly Counter DocumentNotCached = Metrics.CreateCounter("nuviot_document_not_cached", "Document Not Cached.", "entity");

        public DocumentDBRepoBase(Uri endpoint, String sharedKey, String dbName, IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null)
        {
            _endPointString = endpoint.ToString();

            _sharedKey = sharedKey;
            _dbName = dbName;
            _logger = logger;
            _cacheProvider = cacheProvider;
            _dependencyManager = dependencyManager;

            _defaultCollectionName = typeof(TEntity).Name;
            if (!_defaultCollectionName.ToLower().EndsWith("s"))
            {
                _defaultCollectionName += "s";
            }
        }


        public DocumentDBRepoBase(string endpoint, String sharedKey, String dbName, IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null) :
            this(new Uri(endpoint), sharedKey, dbName, logger, cacheProvider, dependencyManager)
        {

        }

        public DocumentDBRepoBase(IAdminLogger logger)
        {
            _logger = logger;

        }

        public void SetConnection(String connectionString, string sharedKey, string dbName)
        {
            _endPointString = connectionString;

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

            _defaultCollectionName = typeof(TEntity).Name;
            if (!_defaultCollectionName.ToLower().EndsWith("s"))
            {
                _defaultCollectionName += "s";
            }
        }

        public async Task DeleteCollectionAsync()
        {
            var client = await GetDocumentClientAsync();
            var database = await GetDatabase(client);
            await database.DeleteAsync();
        }

        public virtual string GetPartitionKey()
        {
            return "/_partitionKey";
        }

        protected async Task<CosmosClient> GetDocumentClientAsync()
        {
            if (_endPointString == null)
            {
                var ex = new ArgumentNullException($"Invalid or missing end point information on {GetType().Name}");
                _logger.AddException($"[{GetType().Name}_GetDocumentClientAsync[", ex);
                throw ex;
            }

            if (String.IsNullOrEmpty(_sharedKey))
            {
                var ex = new ArgumentNullException($"Invalid or missing shared key information on {GetType().Name}");
                _logger.AddException($"[{GetType().Name}_GetDocumentClientAsync]", ex);
                throw ex;
            }

            if (_cosmosClient == null)
            {
                if (_isDBCheckComplete)
                {
                    Console.WriteLine($"[{GetType().Name}__GetDocumentClientAsync - static setting, db has been created, no need to check and create if necessary.");

                    var connectionPolicy = new CosmosClientOptions();
                    _cosmosClient = new CosmosClient(_endPointString, _sharedKey, connectionPolicy);
                }
                else
                {
                    var sw = Stopwatch.StartNew();

                    var connectionPolicy = new CosmosClientOptions();
                    _cosmosClient = new CosmosClient(_endPointString, _sharedKey, connectionPolicy);

                    var dbCreateResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_dbName);
                    if (dbCreateResponse == null)
                    {
                        var ex = new ArgumentNullException($"Could not crate database null response.");
                        _logger.AddException($"[{GetType().Name}_GetDocumentClientAsync]", ex);
                        throw ex;
                    }

                    if (dbCreateResponse.StatusCode != System.Net.HttpStatusCode.OK &&
                       dbCreateResponse.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        var ex = new ArgumentNullException($"Invalid status code from create database - {dbCreateResponse.StatusCode}.");
                        _logger.AddException($"[{GetType().Name}_CGetDocumentClientAsync]", ex);
                        throw ex;
                    }


                    var db = _cosmosClient.GetDatabase(_dbName);
                    var containerResponse = await db.CreateContainerIfNotExistsAsync(GetCollectionName(), GetPartitionKey());
                    if (containerResponse == null)
                    {
                        var ex = new ArgumentNullException($"Could not crate container null response.");
                        _logger.AddException($"[{GetType().Name}_GetDocumentClientAsync]", ex);
                        throw ex;
                    }

                    if (containerResponse.StatusCode != System.Net.HttpStatusCode.OK &&
                       containerResponse.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        var ex = new ArgumentNullException($"Invalid status code from create container - {containerResponse.StatusCode}.");
                        _logger.AddException($"[{GetType().Name}_GetDocumentClientAsync]", ex);
                        throw ex;
                    }

                    Console.WriteLine($"[{GetType().Name}__GetDocumentClientAsync] - Execution Time {sw.Elapsed.TotalMilliseconds}ms");
                    _isDBCheckComplete = true;
                }
            }
            else
            {
              if(_verboseLogging) Console.WriteLine($"[{GetType().Name}__GetDocumentClientAsync] - reuse existing cosmos db connection");
            }            


            return _cosmosClient;
        }

        protected async Task<Container> GetContainerAsync()
        {
            var docClient = await GetDocumentClientAsync();            
            return docClient.GetContainer(_dbName, GetCollectionName());      
        }

        protected virtual bool ShouldConsolidateCollections
        {
            get { return false; }
        }

        protected async Task<Database> GetDatabase(CosmosClient client)
        {
            if (String.IsNullOrEmpty(_dbName))
            {
                var ex = new InvalidOperationException($"Invalid or missing database name information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            return await client.CreateDatabaseIfNotExistsAsync(_dbName);
        }

        public virtual String GetCollectionName()
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
                return _defaultCollectionName;
            }
        }

        protected virtual bool IsRuntimeData { get { return false; } }

        protected async Task<ItemResponse<TEntity>> CreateDocumentAsync(TEntity item)
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

            var container = await GetContainerAsync();

            var sw = Stopwatch.StartNew();
            var response = await container.CreateItemAsync(item);

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                _logger.AddCustomEvent(LogLevel.Error, $"DocuementDbRepo<{_dbName}>_CreateDocumentAsync", "Error return code: " + response.StatusCode,
                    new KeyValuePair<string, string>("EntityType", typeof(TEntity).Name),
                    new KeyValuePair<string, string>("Id", item.Id)
                    );
                throw new Exception("Could not insert entity");
            }
            else
            {
                Console.WriteLine($"{GetType().Name}__{nameof(CreateDocumentAsync)} - Request Cost - {response.RequestCharge} - Elapsed {sw.Elapsed.TotalMilliseconds}ms");

                DocumentInsert.WithLabels(typeof(TEntity).Name).NewTimer();
                DocumentRequestCharge.WithLabels(GetCollectionName()).Set(response.RequestCharge);
            }

            if (_cacheProvider != null)
            {
                await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item));
            }

            return response;
        }

        private string GetCacheKey(string id)
        {
            return $"{_dbName}-{typeof(TEntity).Name}-{id}".ToLower();
        }

        protected async Task<ItemResponse<TEntity>> UpsertDocumentAsync(TEntity item)
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

            var container = await GetContainerAsync();

            var sw = Stopwatch.StartNew();
            var timer = DocumentInsert.WithLabels(typeof(TEntity).Name).NewTimer();

            if (_dependencyManager != null)
            {
                var exisitng = await GetDocumentAsync(item.Id);
                if (exisitng.Name != item.Name)
                {
                    var dependencyResult = await _dependencyManager.CheckForDependenciesAsync(item);
                    if(dependencyResult.IsInUse)
                    {
                        Console.WriteLine($"[DocumentDBRepoBase_UpsertDocumentAsync] - Object {item.Name} in use");
                        foreach (var obj in dependencyResult.DependentObjects)
                            await _dependencyManager.RenameDependentObjectsAsync(item.LastUpdatedBy, item.Id, item.GetType().Name, obj.Id, obj.RecordType, item.Name);
                    }
                }
                else
                {
                    if (_verboseLogging) Console.WriteLine($"[DocumentDBRepoBase_UpsertDocumentAsync] - Object {item.Name} name not changed");
                }
            }
            else
            {
                Console.WriteLine($"[DocumentDBRepoBase_UpsertDocumentAsync] - Dependency Manager is null");
            }


            var upsertResult = await container.UpsertItemAsync(item);
            switch (upsertResult.StatusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();
                    _logger.AddError("[DocumentDBRepoBase_UpsertDocumentAsync]", "BadRequest", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new Exception($"Bad Request on Upsert {typeof(TEntity).Name}");
                case System.Net.HttpStatusCode.Forbidden:
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();
                    _logger.AddError("]DocumentDBRepoBase_UpsertDocumentAsync]", "Forbidden", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new Exception($"Forbidden on Upsert {typeof(TEntity).Name}");
                case System.Net.HttpStatusCode.Conflict:
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();
                    _logger.AddError("[DocumentDBRepoBase_UpsertDocumentAsync]", "Conflict", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new ContentModifiedException()
                    {
                        EntityType = typeof(TEntity).Name,
                        Id = item.Id
                    };

                case System.Net.HttpStatusCode.RequestEntityTooLarge:
                    _logger.AddError("DocumentDBRepoBase_UpsertDocumentAsync", "RequestEntityTooLarge", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                    throw new Exception($"RequestEntityTooLarge Upsert on type {typeof(TEntity).Name}");

                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                    Console.WriteLine($"[DocumentDBRepoBase_UpsertDocumentAsync] Document Update {typeof(TEntity).Name} in {sw.Elapsed.TotalMilliseconds}ms, Resource Charge: {upsertResult.RequestCharge}");
                    break;
            }

            timer.Dispose();
            DocumentRequestCharge.WithLabels(GetCollectionName()).Set(upsertResult.RequestCharge);

            if (_cacheProvider != null)
            {
                await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item));
            }

            return upsertResult;
        }

        protected async Task<TEntity> GetDocumentAsync(string id, bool throwOnNotFound = true)
        {
            if (_cacheProvider != null)
            {
                var sw = Stopwatch.StartNew();
                var json = await _cacheProvider.GetAsync(GetCacheKey(id));
                if (!String.IsNullOrEmpty(json))
                {
                    if (_verboseLogging) Console.WriteLine($"[DocStorage] Get document From Cache {typeof(TEntity).Name} in {sw.Elapsed.TotalMilliseconds}ms");

                    try
                    {
                        var entity = JsonConvert.DeserializeObject<TEntity>(json);
                        if (entity.EntityType != typeof(TEntity).Name)
                        {
                            if (throwOnNotFound)
                            {
                                _logger.AddCustomEvent(LogLevel.Error, "[DocumentDBRepoBase_GetDocumentAsync]", $"Type Mismatch", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Actual Type", entity.EntityType), new KeyValuePair<string, string>("id", id));
                                DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                                throw new RecordNotFoundException(typeof(TEntity).Name, id);
                            }
                            else
                            {
                                return default;
                            }
                        }
                        else
                        {
                            DocumentCacheHit.WithLabels(typeof(TEntity).Name).Inc();
                            return entity;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DocumentDBRepoBase_GetDocumentAsync] Exception Deserializing Object: {GetCacheKey(id)} - {ex.Message}");
                        await _cacheProvider.RemoveAsync(GetCacheKey(id));
                    }
                }
                else
                {
                    DocumentCacheMiss.WithLabels(typeof(TEntity).Name).Inc();
                    if (_verboseLogging) Console.WriteLine($"[DocumentDBRepoBase_GetDocumentAsync] Cache Miss {GetCacheKey(id)}");
                }
            }
            else
            {
                DocumentNotCached.WithLabels(typeof(TEntity).Name).Inc();
            }

            var doc = await GetDocumentAsync(id, null, throwOnNotFound);
            if (_cacheProvider != null)
            {
                var sw = Stopwatch.StartNew();
                await _cacheProvider.AddAsync(GetCacheKey(id), JsonConvert.SerializeObject(doc));
                if (_verboseLogging) Console.WriteLine($"[DocumentDBRepoBase_GetDocumentAsync] Add To Cache {GetCacheKey(id)} - {sw.ElapsedMilliseconds}ms");
            }

            return doc;
        }

        protected async Task<TEntity> GetDocumentAsync(string id, string partitionKey, bool throwOnNotFound = true)
        {
            try
            {
                var container = await GetContainerAsync();

                var sw = Stopwatch.StartNew();
                var timer = DocumentGet.WithLabels(typeof(TEntity).Name).NewTimer();
                var response = await container.ReadItemAsync<TEntity>(id, String.IsNullOrEmpty(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey));
                timer.Dispose();

                if (_verboseLogging) Console.WriteLine($"[DocumentDBRepoBase__GetDocumentAsync] Get document From DocStore {typeof(TEntity).Name} in {sw.Elapsed.TotalMilliseconds}ms, Resource Charge: {response.RequestCharge}");

                if (response == null)
                {
                    _logger.AddCustomEvent(LogLevel.Error, "[DocumentDBRepoBase__GetDocumentAsync]", $"Empty Response", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("id", id));
                    DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }

                DocumentRequestCharge.WithLabels(GetCollectionName()).Set(response.RequestCharge);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var entity = response.Resource;

                    if (entity.EntityType != typeof(TEntity).Name)
                    {
                        if (throwOnNotFound)
                        {
                            _logger.AddCustomEvent(LogLevel.Error, "[DocumentDBRepoBase__GetDocumentAsync]", $"Type Mismatch", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Actual Type", entity.EntityType), new KeyValuePair<string, string>("id", id));
                            DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
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
                    DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                    if (throwOnNotFound)
                    {
                        _logger.AddCustomEvent(LogLevel.Error, "[DocumentDBRepoBase__GetDocumentAsync]", $"Error requesting document", new KeyValuePair<string, string>("Invalid Status Code", response.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
                        throw new RecordNotFoundException(typeof(TEntity).Name, id);
                    }
                    else
                    {
                        return default;
                    }
                }
            }
            catch (CosmosException ex)
            {
                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                _logger.AddCustomEvent(LogLevel.Error, "[DocumentDBRepoBase__GetDocumentAsync]", $"Error requesting document", new KeyValuePair<string, string>("DocumentClientException", ex.Message), new KeyValuePair<string, string>("StatusCode", ex.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
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

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                _logger.AddCustomEvent(LogLevel.Error, "[DocumentDBRepoBase__GetDocumentAsync]", $"Error requesting document", new KeyValuePair<string, string>("Exception", ex.Message), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
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

        protected async Task<ItemResponse<TEntity>> DeleteDocumentAsync(string id)
        {
            var timer = DocumentDelete.WithLabels(typeof(TEntity).Name).NewTimer();
            var doc = await GetDocumentAsync(id);

            if(_dependencyManager != null)
            {
                var dependencyies = await _dependencyManager.CheckForDependenciesAsync(doc);
                if(dependencyies.IsInUse)
                {
                    timer.Dispose();
                    throw new InUseException(dependencyies);
                }
            }

            if (_cacheProvider != null)
            {
                var cacheKey = GetCacheKey(id);
                await _cacheProvider.RemoveAsync(cacheKey);
            }

            var container = await GetContainerAsync();

            var result = await container.DeleteItemAsync<TEntity>(id, PartitionKey.None);
            timer.Dispose();

            return result;
        }

        protected async Task<ItemResponse<TEntity>> DeleteDocumentAsync(string id, string partitionKey)
        {
            var timer = DocumentDelete.WithLabels(typeof(TEntity).Name).NewTimer();
            var doc = await GetDocumentAsync(id, partitionKey);

            if (_dependencyManager != null)
            {
                var dependencyies = await _dependencyManager.CheckForDependenciesAsync(doc);
                if (dependencyies.IsInUse)
                {
                    timer.Dispose();
                    throw new InUseException(dependencyies);
                }
            }

            if (_cacheProvider != null)
            {
                var cacheKey = GetCacheKey(id);
                await _cacheProvider.RemoveAsync(cacheKey);
            }

            var container = await GetContainerAsync();
            var partitionKeyValue = new PartitionKey(partitionKey);
            var response = await container.DeleteItemAsync<TEntity>(id, partitionKeyValue);
            timer.Dispose();
            return response;
        }

        protected async Task<IEnumerable<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query)
        {
            var sw = Stopwatch.StartNew();
            var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

            var items = new List<TEntity>();

            var container = await GetContainerAsync();
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(query)
                    .Where(itm => itm.EntityType == typeof(TEntity).Name);

            var page = 1;

            var requestCharge = 0.0;

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    if (_verboseLogging) Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                    requestCharge += response.RequestCharge;
                    foreach (var item in response)
                    {
                        items.Add(item);
                    }
                }
            }

            timer.Dispose();
            DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

            return items;
        }

        protected async Task<IEnumerable<TEntity>> QueryAsync(string sql, params QueryParameter[] sqlParams)
        {
            var query = new QueryDefinition(sql);

            Console.WriteLine(sql);

            foreach (var param in sqlParams)
            {
                query = query.WithParameter(param.Name, param.Value);
                Console.WriteLine($"\t{param.Name} - {param.Value}");
            }

            var sw = Stopwatch.StartNew();
            var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

            var requestCharge = 0.0;

            var items = new List<TEntity>();

            var container = await GetContainerAsync();
            using (var resultSet = container.GetItemQueryIterator<TEntity>(query))
            {
                var page = 1;
                while (resultSet.HasMoreResults)
                {
                    var response = await resultSet.ReadNextAsync();
                    if (_verboseLogging) Console.WriteLine($"[DocStorage] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                    requestCharge += response.RequestCharge;
                    items.AddRange(response);
                }
            }

            timer.Dispose();
            DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

            return items;
        }

        protected async Task<ListResponse<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntity>();
                var requestCharge = 0.0;

                var container = await GetContainerAsync();
                var linqQuery = container.GetItemLinqQueryable<TEntity>()
                        .Where(query)
                        .Where(itm => itm.EntityType == typeof(TEntity).Name)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;

                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {
                    if (_verboseLogging && !iterator.HasMoreResults)
                        Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge; 
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                var listResponse = ListResponse<TEntity>.Create(listRequest, items);
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException("[DocumentDBBase_QueryAsync] (query, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }


        protected async Task<ListResponse<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, 
                            System.Linq.Expressions.Expression<Func<TEntity, string>> sort, ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntity>();
                var requestCharge = 0.0;

                var container = await GetContainerAsync();
                var linqQuery = container.GetItemLinqQueryable<TEntity>()
                        .Where(query)
                        .Where(itm => itm.EntityType == typeof(TEntity).Name)
                        .OrderBy(sort)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;

                Console.WriteLine($"[DocStorage] Query {page++} Query Document {linqQuery}");

                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {

                    if(_verboseLogging && !iterator.HasMoreResults)
                        Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                var listResponse = ListResponse<TEntity>.Create(listRequest, items);
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                Console.WriteLine(listRequest);
                Console.WriteLine(listResponse);

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException("[DocumentDBBase__QueryAsync] (query, sort, listRquest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        protected async Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary, TEntityFactory>(System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> query,
                           System.Linq.Expressions.Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory : class, ISummaryFactory, INoSQLEntity
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntityFactory>();
                var requestCharge = 0.0;

                var container = await GetContainerAsync();
                var linqQuery = container.GetItemLinqQueryable<TEntityFactory>()
                        .Where(query)
                        .Where(itm => itm.EntityType == typeof(TEntity).Name)
                        .OrderBy(sort)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;

                Console.WriteLine($"[DocStorage] Query {page++} Query Document {linqQuery}");

                using (var iterator = linqQuery.ToFeedIterator<TEntityFactory>())
                {

                    if (_verboseLogging && !iterator.HasMoreResults)
                        Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                var listResponse = ListResponse<TEntitySummary>.Create(listRequest, items.Select(itm=>itm.CreateSummary() as TEntitySummary));
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);
                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException("[DocumentDBBase__QueryAsync] (query, sort, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntitySummary>.Create(new List<TEntitySummary>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        protected async Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TEntitySummary : class
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntitySummary>();
                var requestCharge = 0.0;

                var query = new QueryDefinition(sql);
                
                foreach (var param in sqlParams)
                {
                    query = query.WithParameter(param.Name, param.Value);
                }

                var page = 1;

                Console.WriteLine($"[DocStorage] Query {page++} Query Document {sql}");
                var container = await GetContainerAsync();

                using (var iterator = container.GetItemQueryIterator<TEntitySummary>(query))
                {
                    if (_verboseLogging && !iterator.HasMoreResults)
                        Console.WriteLine($"[DocStorage] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) Console.WriteLine($"[DocStorage] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                var listResponse = ListResponse<TEntitySummary>.Create(listRequest, items);
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);
                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException("[DocumentDBBase__QueryAsync] (query, sort, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntitySummary>.Create(new List<TEntitySummary>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        protected async Task<ListResponse<TEntity>> QueryDescendingAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query,
                          System.Linq.Expressions.Expression<Func<TEntity, string>> sort, ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntity>();
                var requestCharge = 0.0;

                var container = await GetContainerAsync();
                var linqQuery = container.GetItemLinqQueryable<TEntity>()
                        .Where(query)
                        .Where(itm => itm.EntityType == typeof(TEntity).Name)
                        .OrderByDescending(sort)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;

                Console.WriteLine($"[DocStorage] Query {page++} Query Document {linqQuery}");


                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {

                    if (_verboseLogging && !iterator.HasMoreResults)
                        Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }


                var listResponse = ListResponse<TEntity>.Create(listRequest, items);
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                Console.WriteLine(listRequest);
                Console.WriteLine(listResponse);
                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException("[DocumentDBBase__QueryAsync] (query, sort, listRquest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

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
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntity>();
                var container = await GetContainerAsync();
                var linqQuery = container.GetItemLinqQueryable<TEntity>()
                        .Where(query)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var requestCharge = 0.0;

                var page = 1;

                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {
                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                _logger.AddException("[DocumentDBBase_QueryAll] (query, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();


                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        protected async Task<ListResponse<TEntity>> DescOrderQueryAsync<TKey>(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, 
                                                    System.Linq.Expressions.Expression<Func<TEntity, TKey>> orderBy, 
                                                    ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntity>();

                var container = await GetContainerAsync();
                var linqQuery = container.GetItemLinqQueryable<TEntity>()
                        .Where(query)
                        .OrderByDescending(orderBy)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;
                var requestCharge = 0.0;

                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {
                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        Console.WriteLine($"[DocStorage] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                return ListResponse<TEntity>.Create(listRequest, items);
            }
            catch (Exception ex)
            {
                _logger.AddException("DocumentDBBase", ex, typeof(TEntity).Name.ToKVP("entityType"));

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        public void Dispose()
        {
            if(_cosmosClient != null)
            {
                _cosmosClient.Dispose();
                _cosmosClient = null;
            }
        }
    }
}
