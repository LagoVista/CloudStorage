// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 0966a614884bf62f14e46c78019b9280b027724296e08c242d0c640e6d6e7d98
// IndexVersion: 2
// --- END CODE INDEX META ---
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
using System.Text;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Azure;
using System.Reflection;
using LagoVista.Core.Attributes;
using System.Security.Policy;
using LagoVista.Core.Models;
using LagoVista.CloudStorage.Interfaces;

namespace LagoVista.CloudStorage.DocumentDB
{
    public class DocumentDBRepoBase<TEntity> where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity, IRevisionedEntity
    {
        enum StorageProviderTypes
        {
            Original,
            CosmosDB,
            Mongo,
        }

        private string _endPointString;
        private string _sharedKey;
        private string _dbName;
        private string _defaultCollectionName;
        private static CosmosClient _cosmosClient;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IDependencyManager _dependencyManager;

        private readonly IDocumentDBRepoBase<TEntity> _storage;

        private StorageProviderTypes _stoargeProvider = StorageProviderTypes.Original;

        private bool _verboseLogging = false;

        private static bool _isDBCheckComplete = false;

        private static readonly Gauge SQLInsertMetric = Metrics.CreateGauge("sql_insert", "Elapsed time for SQL insert.",
           new GaugeConfiguration
           {
               // Here you specify only the names of the labels.
               LabelNames = new[] { "action" }
           });

        protected static readonly Gauge DocumentRequestCharge = Metrics.CreateGauge("nuviot_document_request_charge", "Elapsed time for document get.", "collection");
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
        protected static readonly Counter DocumentCacheHit = Metrics.CreateCounter("nuviot_document_cache_hit", "Document Cache Hit.", "entity");
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

            _storage = new StorageProviders.CosmosDBStorage<TEntity>(endpoint, sharedKey, dbName, logger, cacheProvider, dependencyManager);

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

        public DocumentDBRepoBase(string endpoint, String sharedKey, String dbName, IDocumentCloudCachedServices cloudServices) : 
            this(endpoint, sharedKey, dbName, cloudServices.AdminLogger, cloudServices.CacheProvider, cloudServices.DependencyManager)
        { 

        }

        public DocumentDBRepoBase(string endpoint, String sharedKey, String dbName, IDocumentCloudServices cloudServices) :
            this(endpoint, sharedKey, dbName, cloudServices.AdminLogger,  dependencyManager:cloudServices.DependencyManager)
        {

        }


        public void SetConnection(String connectionString, string sharedKey, string dbName)
        {
            _endPointString = connectionString;

            _sharedKey = sharedKey;
            if (String.IsNullOrEmpty(_sharedKey))
            {
                var ex = new InvalidOperationException($"Invalid or missing shared key information on {GetType().Name}");
                _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>__CTor]", ex);
                throw ex;
            }

            _dbName = dbName;
            if (String.IsNullOrEmpty(_dbName))
            {
                var ex = new InvalidOperationException($"Invalid or missing database name information on {GetType().Name}");
                _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>__CTor]", ex);
                throw ex;
            }

            _defaultCollectionName = typeof(TEntity).Name;
            if (!_defaultCollectionName.ToLower().EndsWith("s"))
            {
                _defaultCollectionName += "s";
            }

            if (_stoargeProvider == StorageProviderTypes.CosmosDB)
            {
                _storage.SetConnection(connectionString, sharedKey, dbName);
            }
        }

        public async Task DeleteCollectionAsync()
        {
            if (_stoargeProvider == StorageProviderTypes.Original)
            {
                var client = await GetDocumentClientAsync();
                var database = await GetDatabase(client);
                await database.DeleteAsync();
            }
            else
            {
                await _storage.DeleteCollectionAsync();
            }
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
                _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>__GetDocumentClientAsync]", ex);
                throw ex;
            }

            if (String.IsNullOrEmpty(_sharedKey))
            {
                var ex = new ArgumentNullException($"Invalid or missing shared key information on {GetType().Name}");
                _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>__GetDocumentClientAsync]", ex);
                throw ex;
            }

            if (_cosmosClient == null)
            {
                if (_isDBCheckComplete)
                {
                    _logger.Trace($"[DocumentDbRepo<{typeof(TEntity).Name}>__GetDocumentClientAsync] - static setting, db has been created, no need to check and create if necessary.");

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
                        _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>__etDocumentClientAsync]", ex);
                        throw ex;
                    }

                    if (dbCreateResponse.StatusCode != System.Net.HttpStatusCode.OK &&
                       dbCreateResponse.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        var ex = new ArgumentNullException($"Invalid status code from create database - {dbCreateResponse.StatusCode}.");
                        _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>____GetDocumentClientAsync]", ex);
                        throw ex;
                    }


                    var db = _cosmosClient.GetDatabase(_dbName);
                    var containerResponse = await db.CreateContainerIfNotExistsAsync(GetCollectionName(), GetPartitionKey());
                    if (containerResponse == null)
                    {
                        var ex = new ArgumentNullException($"Could not crate container null response.");
                        _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>__GetDocumentClientAsync]", ex);
                        throw ex;
                    }

                    if (containerResponse.StatusCode != System.Net.HttpStatusCode.OK &&
                       containerResponse.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        var ex = new ArgumentNullException($"Invalid status code from create container - {containerResponse.StatusCode}.");
                        _logger.AddException($"[DocumentDbRepo<{typeof(TEntity).Name}>__GetDocumentClientAsync]", ex);
                        throw ex;
                    }

                    _logger.Trace($"[DocumentDbRepo<{typeof(TEntity).Name}>__GetDocumentClientAsync] - Execution Time {sw.Elapsed.TotalMilliseconds}ms");
                    _isDBCheckComplete = true;
                }
            }
            else
            {
                if (_verboseLogging) _logger.Trace($"[DocumentDbRepo<{typeof(TEntity).Name}>__GetDocumentClientAsync] - reuse existing cosmos db connection");
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
                _logger.AddException($"[{GetType().Name}_CTor]", ex);
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

        protected async Task<OperationResponse<TEntity>> CreateDocumentAsync(TEntity item)
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

                _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDbRepo<{typeof(TEntity).Name}>__CreateDocumentAsync]", "Error return code: " + response.StatusCode,
                    new KeyValuePair<string, string>("EntityType", typeof(TEntity).Name),
                    new KeyValuePair<string, string>("Id", item.Id)
                    );
                throw new Exception("Could not insert entity");
            }
            else
            {
                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__{nameof(CreateDocumentAsync)}] - Request Cost - {response.RequestCharge} - Elapsed {sw.Elapsed.TotalMilliseconds}ms");

                DocumentInsert.WithLabels(typeof(TEntity).Name).NewTimer();
                DocumentRequestCharge.WithLabels(GetCollectionName()).Set(response.RequestCharge);
            }

            if (_cacheProvider != null)
            {
                await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item));
            }

            return new OperationResponse<TEntity>(response);
        }

        private async Task PostDiscussionUpdates(IDiscussableEntity entity)
        {            
            var discussable = entity as IDiscussableEntity;
            var mentionRegEx = new Regex(@"data-mention-id=""(?<mentionId>[A-F0-9]+)""");
            var forMAttr = typeof(TEntity).GetCustomAttribute<EntityDescriptionAttribute>();
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                foreach (var discussion in discussable.Discussions)
                {
                    if (!String.IsNullOrEmpty(discussion.Note))
                    {
                        foreach (Match match in mentionRegEx.Matches(discussion.Note))
                        {
                            var inputBytes = System.Text.Encoding.ASCII.GetBytes(discussion.Note);
                            var hashBytes = md5.ComputeHash(inputBytes);
                            var hash = System.Convert.ToBase64String(hashBytes);
                            if (!discussion.Handled || discussion.NoteHash != hash)
                            {
                                Console.WriteLine($"===> 1) Discussion {discussion.Id} Handled {discussion.Handled}, Note Hash {discussion.NoteHash}, Hash {hash}");
                                await UserNotificationServiceProvider.Instance.QueueDiscussionNotificationAsync(match.Groups["mentionId"].Value, entity, discussion);
                                discussion.Handled = true;
                                discussion.NoteHash = hash;
                                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__{nameof(PostDiscussionUpdates)}_Discussion] - {entity.Name}");
                                Console.WriteLine($"===> 2) Discussion {discussion.Id} Handled {discussion.Handled}, Note Hash {discussion.NoteHash}, Hash {hash}");
                            }
                        }

                        foreach (var response in discussion.Responses)
                        {
                            foreach (Match responseMatch in mentionRegEx.Matches(response.Note))
                            {
                                var inputBytes = System.Text.Encoding.ASCII.GetBytes(response.Note);
                                var hashBytes = md5.ComputeHash(inputBytes);
                                var hash = System.Convert.ToBase64String(hashBytes);
                                if (!response.Handled || response.NoteHash != hash)
                                {
                                    Console.WriteLine($"===> 1) Response {response.Id} Handled {response.Handled}, Note Hash {response.NoteHash}, Hash {hash}");
                                    await UserNotificationServiceProvider.Instance.QueueDiscussionNotificationAsync(responseMatch.Groups["mentionId"].Value, entity, discussion, response);
                                    response.Handled = true;
                                    response.NoteHash = hash;
                                    _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__{nameof(PostDiscussionUpdates)}_Response] - {entity.Name}");
                                    Console.WriteLine($"===> 2) Response {response.Id} Handled {response.Handled}, Note Hash {response.NoteHash}, Hash {hash}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private string GetCacheKey(string id)
        {
            return $"{_dbName}-{typeof(TEntity).Name}-{id}".ToLower();
        }

        protected async Task<OperationResponse<TEntity>> UpsertDocumentAsync(TEntity item)
        {
            if (item is IValidateable)
            {
                var result = Validator.Validate(item as IValidateable);
                if (!result.Successful)
                {
                    throw new ValidationException("Invalid Data.", result.Errors);
                }
            }

            item.Revision++;
            item.RevisionTimeStamp = DateTime.UtcNow.ToJSONString();

            item.DatabaseName = _dbName;
            item.EntityType = typeof(TEntity).Name;

            var container = await GetContainerAsync();

            var sw = Stopwatch.StartNew();
            var timer = DocumentUpdate.WithLabels(typeof(TEntity).Name).NewTimer();

            if (_dependencyManager != null)
            {
                var exisitng = await GetDocumentAsync(item.Id);
                if (exisitng.Name != item.Name)
                {
                    var dependencyResult = await _dependencyManager.CheckForDependenciesAsync(item);
                    if (dependencyResult.IsInUse)
                    {
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] - Object {item.Name} in use");
                        foreach (var obj in dependencyResult.DependentObjects)
                            await _dependencyManager.RenameDependentObjectsAsync(item.LastUpdatedBy, item.Id, item.GetType().Name, obj.Id, obj.RecordType, item.Name);
                    }

                    item.AuditHistory.Add(new Core.Models.EntityChangeSet()
                    {
                        ChangeDate = DateTime.UtcNow.ToJSONString(),
                        ChangedBy = item.LastUpdatedBy,
                        Changes = new List<Core.Models.EntityChange>()
                        {
                            new Core.Models.EntityChange()
                            {
                                OldValue = exisitng.Name,
                                NewValue = item.Name,
                                Field = nameof(item.Name),
                            }
                        }
                    });

                    await _dependencyManager.RenameObjectAsync(item.LastUpdatedBy, item.Id, item.GetType().Name, item.Name);
                }
                else
                {
                    if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] - Object {item.Name} name not changed");
                }
            }
            else
            {
                if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] - Dependency Manager is null");
            }

            var discussable = item as IDiscussableEntity;

            if (discussable != null)
            {
                Console.WriteLine($"===================> Checking it is discussable <========================================");
                
                await PostDiscussionUpdates(discussable);
            }

            var upsertResult = await container.UpsertItemAsync(item);
            switch (upsertResult.StatusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();
                    _logger.AddError($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync]", "BadRequest", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new Exception($"Bad Request on Upsert {typeof(TEntity).Name}");

                case System.Net.HttpStatusCode.Forbidden:
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();
                    _logger.AddError($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync]", "Forbidden", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new Exception($"Forbidden on Upsert {typeof(TEntity).Name}");

                case System.Net.HttpStatusCode.Conflict:
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();
                    _logger.AddError($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync]", "Conflict", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    throw new ContentModifiedException()
                    {
                        EntityType = typeof(TEntity).Name,
                        Id = item.Id
                    };

                case System.Net.HttpStatusCode.RequestEntityTooLarge:
                    _logger.AddError($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync", "RequestEntityTooLarge]", typeof(TEntity).Name.ToKVP("entityType"), item.Id.ToKVP("id"));
                    DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                    throw new Exception($"RequestEntityTooLarge Upsert on type {typeof(TEntity).Name}");

                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                    _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] Document Update {typeof(TEntity).Name} in {sw.Elapsed.TotalMilliseconds}ms, Resource Charge: {upsertResult.RequestCharge}");
                    break;
            }

            timer.Dispose();
            DocumentRequestCharge.WithLabels(GetCollectionName()).Set(upsertResult.RequestCharge);

            if (_cacheProvider != null)
            {
                await _cacheProvider.RemoveAsync(GetCacheKey(item.Id));
                sw.Restart();
                await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item));
                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] Added {typeof(TEntity).Name} back to cache after update in {sw.Elapsed.TotalMilliseconds}ms");
            }

            return new OperationResponse<TEntity>(upsertResult);
        }

        protected async Task<TEntity> GetDocumentAsync(string id, bool throwOnNotFound = true)
        {
            var sw = Stopwatch.StartNew();

            if (_cacheProvider != null)
            {
                var json = await _cacheProvider.GetAsync(GetCacheKey(id));
                if (!String.IsNullOrEmpty(json))
                {
                    try
                    {
                        var entity = JsonConvert.DeserializeObject<TEntity>(json);
                        if (entity.EntityType != typeof(TEntity).Name)
                        {
                            if (throwOnNotFound)
                            {
                                _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"Type Mismatch", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Actual Type", entity.EntityType), new KeyValuePair<string, string>("id", id));
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
                            _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync] Get document [{entity.Name}], Org: {entity.OwnerOrganization?.Text} From Cache {typeof(TEntity).Name} in {sw.Elapsed.TotalMilliseconds}ms");
                            return entity;
                        }
                    }
                    catch (Exception ex)
                    {
                        DocumentErrors.Inc();
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync] Exception Deserializing Object: {typeof(TEntity).Name} {GetCacheKey(id)} - {ex.Message}");
                        await _cacheProvider.RemoveAsync(GetCacheKey(id));
                    }
                }
                else
                {
                    DocumentCacheMiss.WithLabels(typeof(TEntity).Name).Inc();
                    _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync] Cache Miss {typeof(TEntity).Name} {GetCacheKey(id)}");
                }
            }
            else
            {
                DocumentNotCached.WithLabels(typeof(TEntity).Name).Inc();
            }

            sw = Stopwatch.StartNew();
            var doc = await GetDocumentAsync(id, null, throwOnNotFound);
            if (_cacheProvider != null)
            {
                sw = Stopwatch.StartNew();
                await _cacheProvider.AddAsync(GetCacheKey(id), JsonConvert.SerializeObject(doc));
                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync] Added To Cache {typeof(TEntity).Name}{GetCacheKey(id)} - {sw.ElapsedMilliseconds}ms");
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


                if (response == null)
                {
                    _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"Empty Response", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("id", id));
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
                            _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"Type Mismatch", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("Actual Type", entity.EntityType), new KeyValuePair<string, string>("id", id));
                            DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                            throw new RecordNotFoundException(typeof(TEntity).Name, id);
                        }
                        else
                        {
                            return default;
                        }
                    }

                    _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync] Load document [{entity.Name}], Org: {entity.OwnerOrganization?.Text} from storage in {sw.Elapsed.TotalMilliseconds}ms, Resource Charge: {response.RequestCharge}",
                        sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"), response.RequestCharge.ToString().ToKVP("requestCharge"), id.ToKVP("recordId"), entity.Name.ToKVP("entityName"));

                    return entity;
                }
                else
                {
                    DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                    if (throwOnNotFound)
                    {
                        _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"Error requesting document", new KeyValuePair<string, string>("Invalid Status Code", response.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
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

                _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"Error requesting document", new KeyValuePair<string, string>("DocumentClientException", ex.Message), new KeyValuePair<string, string>("StatusCode", ex.StatusCode.ToString()), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
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
                _logger.Trace(ex.Message);
                _logger.Trace(ex.StackTrace);
                Console.ResetColor();

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"Error requesting document", new KeyValuePair<string, string>("Exception", ex.Message), new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("Id", id));
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

        protected async Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id)
        {
            var sw = Stopwatch.StartNew();

            var timer = DocumentDelete.WithLabels(typeof(TEntity).Name).NewTimer();
            var doc = await GetDocumentAsync(id);

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

            ItemResponse<TEntity> result;

            if (doc.IsDeleted.HasValue && doc.IsDeleted.Value)
            {
                result = await container.DeleteItemAsync<TEntity>(doc.Id, PartitionKey.None);
            }
            else
            {
                doc.IsDeleted = true;
                doc.DeletionDate = DateTime.UtcNow.ToJSONString();
                result = await container.UpsertItemAsync(doc);
            }
            timer.Dispose();

            _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__DeleteDocumentAsync]", $"Deleted Document {id} in {sw.Elapsed.TotalMilliseconds} ms",
                new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("recordId", id));

            return new OperationResponse<TEntity>(result);
        }

        protected async Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id, string partitionKey)
        {
            var sw = Stopwatch.StartNew();

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


            doc.IsDeleted = true;
            doc.DeletionDate = DateTime.UtcNow.ToJSONString();
            var result = await container.UpsertItemAsync(doc, partitionKeyValue);
            timer.Dispose();

            _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__DeleteDocumentAsync]", $"Deleted Document {id}, partition key {partitionKey} in {sw.Elapsed.TotalMilliseconds} ms",
                new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("recordId", id));

            return new OperationResponse<TEntity>(result);
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
                    if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                    requestCharge += response.RequestCharge;
                    foreach (var item in response)
                    {
                        items.Add(item);
                    }
                }
            }

            timer.Dispose();
            DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

            _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync]", $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] in {sw.Elapsed.TotalMilliseconds} ms",
                new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), linqQuery.ToString().ToKVP("linqQuery"));


            return items;
        }

        protected async Task<IEnumerable<TEntity>> QueryAsync(string sql, params QueryParameter[] sqlParams)
        {
            var query = new QueryDefinition(sql);


            var bldr = new StringBuilder();
            bldr.AppendLine(sql);


            foreach (var param in sqlParams)
            {
                query = query.WithParameter(param.Name, param.Value);
                bldr.Append($"{param.Name}={param.Value};");
            }

            _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] {bldr}");

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
                    if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                    requestCharge += response.RequestCharge;
                    items.AddRange(response);
                }
            }

            timer.Dispose();
            DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

            _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync]", $"Sql query in {sw.Elapsed.TotalMilliseconds} ms",
                new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), sql.ToKVP("sql"));

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
                        .Where(itm => itm.EntityType == typeof(TEntity).Name && (itm.IsDeleted.IsNull() || !itm.IsDeleted.HasValue || !itm.IsDeleted.Value || listRequest.ShowDeleted) &&
                                       (itm.IsDraft == false || listRequest.ShowDrafts))
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;

                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {
                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
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

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync__ListRequest]", $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync__ListRequest] in {sw.Elapsed.TotalMilliseconds} ms",
                    new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), linqQuery.ToString().ToKVP("linqQuery"));

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] (query, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

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
                        .Where(itm => itm.EntityType == typeof(TEntity).Name && (itm.IsDeleted.IsNull() || !itm.IsDeleted.HasValue || !itm.IsDeleted.Value || listRequest.ShowDeleted)
                                         && (itm.IsDraft == false || listRequest.ShowDrafts))
                        .OrderBy(sort)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;

                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QUeryAsync] Query {page++} Query Document {linqQuery}");

                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {

                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QUeryAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
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

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync__ListRequest__Sorted]", 
                    $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync__ListRequest__Sorted] in {sw.Elapsed.TotalMilliseconds} ms",
                    items.Count.ToString().ToKVP("recordCount"),
                    new KeyValuePair<string, string>("recordType", typeof(TEntity).Name), linqQuery.ToString().ToKVP("linqQuery"));


                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync] (query, sort, listRquest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        protected async Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary, TEntityFactory>(System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> query,
                           System.Linq.Expressions.Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory : class, ICategorized, ISummaryFactory, INoSQLEntity, INamedEntity, IRatedEntity, IAuditableEntity
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TEntityFactory>();
                var requestCharge = 0.0;

                if(listRequest.OrderBy != null && listRequest.OrderByDesc != null)
                {
                    return ListResponse<TEntitySummary>.FromError("order by AND order by desc were both provided, must either be both empty or only provide one of the two.");
                }

                if(listRequest.OrderBy != null)
                {
                    switch (listRequest.OrderBy.Value)
                    {
                        case OrderByTypes.Name:
                            sort = (ele => ele.Name);
                            break;
                        case OrderByTypes.Rating:
                            sort = (ele => ele.Stars.ToString());
                            break;
                        case OrderByTypes.CreationDate:
                            sort = (ele => ele.CreationDate);
                            break;
                        case OrderByTypes.LastUpdateDate:
                            sort = (ele => ele.LastUpdatedDate);
                            break;
                    }
                }

                System.Linq.Expressions.Expression<Func<TEntityFactory, string>> orderByDesc = null;

                if (listRequest.OrderByDesc != null)
                {
                    switch (listRequest.OrderByDesc.Value)
                    {
                        case OrderByTypes.Name:
                            orderByDesc = (ele => ele.Name);
                            break;
                        case OrderByTypes.Rating:
                            orderByDesc = (ele => ele.Stars.ToString());
                            break;
                        case OrderByTypes.CreationDate:
                            orderByDesc = (ele => ele.CreationDate);
                            break;
                        case OrderByTypes.LastUpdateDate:
                            orderByDesc = (ele => ele.LastUpdatedDate);
                            break;
                    }
                }

                System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> entityTypeQuery = (qry) => qry.EntityType == typeof(TEntity).Name;
                System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> isDeletedQuery = qry => !qry.IsDeleted.IsDefined() || qry.IsDeleted == false;
                if (listRequest.ShowDeleted)
                    isDeletedQuery = qry => true;

                System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> isDraftQuery = (qry) => !qry.IsDraft.IsDefined() || qry.IsDraft == false;
                if(listRequest.ShowDrafts)
                    isDraftQuery = qry => true;

                System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> categoryQuery = (qry) => qry.Category.Key == listRequest.CategoryKey;;
                if (String.IsNullOrEmpty(listRequest.CategoryKey))
                    categoryQuery = qry => true;

                var container = await GetContainerAsync();
                var baseQuery = container.GetItemLinqQueryable<TEntityFactory>();

                var linqQuery = container.GetItemLinqQueryable<TEntityFactory>()
                                                        .Where(query)
                                                        .Where(entityTypeQuery)
                                                        .Where(categoryQuery)
                                                        .Where(isDeletedQuery)
                                                        .Where(isDraftQuery); 
                                     
                if(orderByDesc != null)
                    linqQuery = linqQuery.OrderByDescending(sort);

                linqQuery = linqQuery.Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                                     .Take(listRequest.PageSize);

                var page = 1;

                using (var iterator = linqQuery.ToFeedIterator<TEntityFactory>())
                {

                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                var listResponse = ListResponse<TEntitySummary>.Create(listRequest, items.Select(itm => itm.CreateSummary() as TEntitySummary));
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                var categories = listResponse.Model.Where(itm => !String.IsNullOrEmpty(itm.CategoryKey)).ToList();
                var groupedCategories = categories.Select(itm => EnumDescription.Create(itm.CategoryId, itm.CategoryKey, itm.Category)).GroupBy(itm => itm.Id);
                listResponse.Categories = groupedCategories.Select(itm => itm.First()).ToList();
                listResponse.Categories.Insert(0, EnumDescription.CreateSelect("-select category-"));

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync]", $"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] in {sw.Elapsed.TotalMilliseconds} ms",
                        items.Count.ToString().ToKVP("recordCount"),
                        new KeyValuePair<string, string>("recordType", typeof(TEntity).Name), linqQuery.ToString().ToKVP("linqQuery"));

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] (query, sort, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntitySummary>.Create(new List<TEntitySummary>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }


        protected async Task<ListResponse<TEntitySummary>> QuerySummaryDescendingAsync<TEntitySummary, TEntityFactory>(System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> query,
                   System.Linq.Expressions.Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory : class, ISummaryFactory, INoSQLEntity, ICategorized, IAuditableEntity
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
                        .Where(itm => String.IsNullOrEmpty(listRequest.CategoryKey) || itm.Category.Key == listRequest.CategoryKey)
                        .Where(itm => itm.EntityType == typeof(TEntity).Name && (itm.IsDeleted.IsNull() || !itm.IsDeleted.HasValue || !itm.IsDeleted.Value || listRequest.ShowDeleted) && (itm.IsDraft == false || listRequest.ShowDrafts))
                        .OrderByDescending(sort)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;



                using (var iterator = linqQuery.ToFeedIterator<TEntityFactory>())
                {

                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryDescendingAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryDescendingAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                var listResponse = ListResponse<TEntitySummary>.Create(listRequest, items.Select(itm => itm.CreateSummary() as TEntitySummary));
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);
                listResponse.Categories = listResponse.Model.Where(itm => !String.IsNullOrEmpty(itm.CategoryKey)).Select(itm => EnumDescription.Create(itm.CategoryId, itm.CategoryKey, itm.Category)).GroupBy(itm => itm.Id).Select(itm => itm.First()).ToList();
                if (listResponse.Categories.Any())
                {
                    listResponse.Categories.Insert(0, EnumDescription.CreateSelect("-select category-"));
                }

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryDescendingAsync]", $"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryDescendingAsync] in {sw.Elapsed.TotalMilliseconds} ms",
                        items.Count.ToString().ToKVP("recordCount"),
                        new KeyValuePair<string, string>("recordType", typeof(TEntity).Name), linqQuery.ToString().ToKVP("linqQuery"));


                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryDescendingAsync] (query, sort, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntitySummary>.Create(new List<TEntitySummary>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        public async Task<ListResponse<TEntity>> QueryAsync(string sql, ListRequest listRequest, params QueryParameter[] sqlParams)
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

            var listResponse = ListResponse<TEntity>.Create(listRequest, items);

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
            return listResponse;
        }

        protected async Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TEntitySummary : class, ISummaryData
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


                var container = await GetContainerAsync();

                using (var iterator = container.GetItemQueryIterator<TEntitySummary>(query))
                {
                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] Query {page++} Query Document {sql}; Timing {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {requestCharge}");

                var listResponse = ListResponse<TEntitySummary>.Create(listRequest, items);
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                listResponse.Categories = listResponse.Model.Where(itm => !String.IsNullOrEmpty(itm.CategoryKey)).Select(itm => EnumDescription.Create(itm.CategoryId, itm.CategoryKey, itm.Category)).GroupBy(itm => itm.Id).Select(itm => itm.First()).ToList();
                if (listResponse.Categories.Any())
                {
                    listResponse.Categories.Insert(0, EnumDescription.CreateSelect("-select category-"));
                }

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync] (query, sort, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

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
                        .Where(itm => itm.EntityType == typeof(TEntity).Name && (!itm.IsDeleted.HasValue || !itm.IsDeleted.Value || listRequest.ShowDeleted) && (itm.IsDraft == false || listRequest.ShowDrafts))
                        .OrderByDescending(sort)
                        .Skip(Math.Max(0, (listRequest.PageIndex - 1)) * listRequest.PageSize)
                        .Take(listRequest.PageSize);

                var page = 1;


                using (var iterator = linqQuery.ToFeedIterator<TEntity>())
                {

                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryDescendingAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryDescendingAsync] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryDescendingAsync] Query {page++} Query Document {linqQuery}; Timing {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {requestCharge}");


                var listResponse = ListResponse<TEntity>.Create(listRequest, items);

                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                _logger.Trace(listRequest.ToString());
                _logger.Trace(listResponse.ToString());
                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryDescendingAsync] (query, sort, listRquest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        public async Task<ListResponse<TMiscEntity>> QueryAsync<TMiscEntity>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TMiscEntity : class
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TMiscEntity>();
                var requestCharge = 0.0;

                var query = new QueryDefinition(sql);

                foreach (var param in sqlParams)
                {
                    query = query.WithParameter(param.Name, param.Value);
                }

                var page = 1;
                var container = await GetContainerAsync();
                using (var iterator = container.GetItemQueryIterator<TMiscEntity>(query))
                {
                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                var listResponse = ListResponse<TMiscEntity>.Create(listRequest, items);
                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                _logger.Trace($"[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] QUery {sql}, Record Count: {items.Count} in {sw.Elapsed.TotalMilliseconds}ms");
                foreach (var param in sqlParams)
                {
                    _logger.Trace($"\t\t[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] {sql}");
                    _logger.Trace($"\t\t[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] {param}");
                }

                _logger.Trace("--");


                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<MiscEntity>] (query, sort, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                var listResponse = ListResponse<TMiscEntity>.Create(new List<TMiscEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        public async Task<List<TMiscEntity>> QueryAsync<TMiscEntity>(string sql, params QueryParameter[] sqlParams) where TMiscEntity : class
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var items = new List<TMiscEntity>();
                var requestCharge = 0.0;

                var query = new QueryDefinition(sql);

                foreach (var param in sqlParams)
                {
                    query = query.WithParameter(param.Name, param.Value);
                }

                var page = 1;

                _logger.Trace($"[DocStorage__QueryAsync<TMiscEntity>]");
                foreach (var param in sqlParams)
                {
                    _logger.Trace($"\t\t[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] {sql}");
                    _logger.Trace($"\t\t[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] {param}");
                }

                var container = await GetContainerAsync();


                using (var iterator = container.GetItemQueryIterator<TMiscEntity>(query))
                {
                    if (_verboseLogging && !iterator.HasMoreResults)
                        _logger.Trace($"[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms");

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        if (_verboseLogging) _logger.Trace($"[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                        requestCharge += response.RequestCharge;
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                }

                timer.Dispose();
                DocumentRequestCharge.WithLabels(typeof(TEntity).Name).Set(requestCharge);

                _logger.Trace($"\t\t[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<TMiscEntity>] Record Count: {items.Count} in {sw.Elapsed.TotalMilliseconds}ms");
                _logger.Trace("--");


                return items;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TMiscEntity).Name}>__QueryAsync<MiscEntity>] (query, sort, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

                throw;
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
            if (_stoargeProvider == StorageProviderTypes.CosmosDB)
                return await _storage.QueryAllAsync(query, listRequest);

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
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAllAsync]  Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
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
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAllAsync] (query, listRequest)", ex, typeof(TEntity).Name.ToKVP("entityType"));

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
            if (_stoargeProvider == StorageProviderTypes.CosmosDB)
                return await _storage.DescOrderQueryAsync(query, orderBy, listRequest);

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
                        _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__DescOrderQueryAsync<TKey>] Page {page++} Query Document {linqQuery} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
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
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__DescOrderQueryAsync<TKey>]", ex, typeof(TEntity).Name.ToKVP("entityType"));

                var listResponse = ListResponse<TEntity>.Create(new List<TEntity>());
                listResponse.Errors.Add(new ErrorMessage(ex.Message));
                return listResponse;
            }
        }

        //public void Dispose()
        //{
        //    if (_cosmosClient != null)
        //    {
        //        _cosmosClient.Dispose();
        //        _cosmosClient = null;
        //    }
        //}

        protected bool Verbose
        {
            get => _verboseLogging;
            set => _verboseLogging = value;
        }
    }
}
