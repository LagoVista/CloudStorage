// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 0966a614884bf62f14e46c78019b9280b027724296e08c242d0c640e6d6e7d98
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core;
using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Attributes;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static LagoVista.CloudStorage.Storage.CosmosSyncRepository;

namespace LagoVista.CloudStorage.DocumentDB
{
    public class DocumentDBRepoBase<TEntity> where TEntity : class, IEntityBase
    {
        private string _endPointString;
        private string _sharedKey;
        private string _dbName;
        private string _defaultCollectionName;
        private readonly ICosmosClientProvider _cosmosClientProvider;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver = new DocumentCollectionNameResolver();
        private readonly CosmosDocumentCollectionProvisioner _cosmosCollectionProvisioner = new CosmosDocumentCollectionProvisioner();
        private readonly IDocumentStorageClient _storageClient;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICacheAborter _cacheAborter;
        private readonly IDependencyManager _dependencyManager;
        private readonly IRagIndexingServices _ragIndexingServices;
        private readonly IProducedArtifactService _producedArtifactService;
        private readonly IFkIndexTableWriterBatched _fkeyIndexWriter;
        private readonly IEntityListCacheInvalidator _entityListCacheInvalidator;

        private bool _verboseLogging = false;


        private static readonly Gauge SQLInsertMetric = Metrics.CreateGauge("sql_insert", "Elapsed time for SQL insert.",
           new GaugeConfiguration
           {
               LabelNames = new[] { "action" }
           });

        protected static readonly Gauge DocumentRequestCharge = Metrics.CreateGauge("nuviot_document_request_charge", "Elapsed time for document get.", "collection");
        protected static readonly Histogram DocumentGet = Metrics.CreateHistogram("nuviot_document_get", "Elapsed time for document get.",
          new HistogramConfiguration
          {
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram DocumentInsert = Metrics.CreateHistogram("nuviot_document_insert", "Elapsed time for document insert.",
          new HistogramConfiguration
          {
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram DocumentUpdate = Metrics.CreateHistogram("nuviot_document_update", "Elapsed time for document update.",
          new HistogramConfiguration
          {
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram DocumentDelete = Metrics.CreateHistogram("nuviot_document_delete", "Elapsed time for document delete.",
          new HistogramConfiguration
          {
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram DocumentQuery = Metrics.CreateHistogram("nuviot_document_query", "Elapsed time for document query.",
          new HistogramConfiguration
          {
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });


        protected static readonly Counter DocumentErrors = Metrics.CreateCounter("nuviot_document_errors", "Error count in document store.", "entity");
        protected static readonly Counter DocumentNotFound = Metrics.CreateCounter("nuviot_document_record_not_found", "Record not found count.", "entity");
        protected static readonly Counter DocumentCacheHit = Metrics.CreateCounter("nuviot_document_cache_hit", "Document Cache Hit.", "entity");
        protected static readonly Counter DocumentCacheMiss = Metrics.CreateCounter("nuviot_document_cache_miss", "Document Cache Miss.", "entity");
        protected static readonly Counter DocumentNotCached = Metrics.CreateCounter("nuviot_document_not_cached", "Document Not Cached.", "entity");

        private DocumentDBRepoBase(IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null, IFkIndexTableWriterBatched fkWriter = null, IDocumentStorageClientProvider documentStorageClientProvider = null)
        {
            _logger = logger;
            _cacheProvider = cacheProvider;
            _dependencyManager = dependencyManager;
            _fkeyIndexWriter = fkWriter;
            if (documentStorageClientProvider == null) throw new ArgumentNullException(nameof(documentStorageClientProvider));
            _storageClient = documentStorageClientProvider.GetClient() ?? throw new InvalidOperationException("Document storage client provider returned null.");

            _defaultCollectionName = typeof(TEntity).Name;
            if (!_defaultCollectionName.ToLower().EndsWith("s"))
            {
                _defaultCollectionName += "s";
            }
        }


        public DocumentDBRepoBase(IDocumentCloudCachedServices cloudServices) :
            this(cloudServices.AdminLogger, cloudServices.CacheProvider, cloudServices.DependencyManager, fkWriter: cloudServices.FkIndexTableWriter, documentStorageClientProvider: cloudServices.DocumentStorageClientProvider)
        {
            _ragIndexingServices = cloudServices.RagIndexingServices;
            _cacheAborter = cloudServices.CacheAborter;
            _producedArtifactService = cloudServices.ProducedArtifactService;
            _entityListCacheInvalidator = cloudServices.EntityListCacheInvalidator;
        }

        public DocumentDBRepoBase(IDocumentCloudServices cloudServices) :
            this(cloudServices.AdminLogger, dependencyManager: cloudServices.DependencyManager, fkWriter: cloudServices.FkIndexTableWriter, documentStorageClientProvider: cloudServices.DocumentStorageClientProvider)
        {
            _fkeyIndexWriter = cloudServices.FkIndexTableWriter;
            _producedArtifactService = cloudServices.ProducedArtifactService;
        }

        private async Task DeleteCollectionAsync()
        {
            var container = await GetContainerAsync();
            await container.DeleteContainerAsync();
        }

        public virtual string GetPartitionKey()
        {
            return EntityDocumentStoragePolicy.CosmosPartitionKeyPath;
        }

        private Task<CosmosClient> GetDocumentClientAsync()
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

            return Task.FromResult(_cosmosClientProvider.GetClient(_endPointString, _sharedKey));
        }

        private async Task<Container> GetContainerAsync()
        {
            var docClient = await GetDocumentClientAsync();
            var collectionName = GetCollectionName();
            await _cosmosCollectionProvisioner.EnsureExistsAsync(docClient, _endPointString, _dbName, collectionName, EntityDocumentStoragePolicy.CosmosPartitionKeyPath).ConfigureAwait(false);
            return docClient.GetContainer(_dbName, collectionName);
        }

        public virtual String GetCollectionName()
        {
            return _collectionNameResolver.Resolve(_dbName, typeof(TEntity));
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

            EntityDocumentStoragePolicy.ValidateForWrite(item);

            item.DatabaseName = _dbName;
            item.EntityType = typeof(TEntity).Name;

            var sw = Stopwatch.StartNew();
            item.SetHash();

            var response = await _storageClient.CreateDocumentAsync(item).ConfigureAwait(false);

            if (_ragIndexingServices != null && (item is IRagableEntity || item.ShouldVectorIndex))
                await _ragIndexingServices.IndexAsync(item);

            if (_producedArtifactService != null)
                await _producedArtifactService.CreateProducedArtifactsAsync(item);

            DocumentInsert.WithLabels(typeof(TEntity).Name).Observe(sw.Elapsed.TotalSeconds);
            _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__{nameof(CreateDocumentAsync)}] Stored document in {sw.Elapsed.TotalMilliseconds}ms");

            if (_cacheProvider != null && (_cacheAborter != null && !_cacheAborter.AbortCache))
            {
                await _cacheProvider.AddAsync(GetCacheKey(item.Id), JsonConvert.SerializeObject(item));
            }

            return response;
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

        private async Task InvalidateEntityListCacheAsync(string orgId)
        {
            if (_entityListCacheInvalidator == null || String.IsNullOrWhiteSpace(orgId))
                return;

            try
            {
                await _entityListCacheInvalidator.InvalidateAsync(orgId, typeof(TEntity));
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__InvalidateEntityListCacheAsync]", ex,
                    typeof(TEntity).Name.ToKVP("entityType"),
                    orgId.ToKVP("orgId"));
            }
        }

        protected async Task<OperationResponse<TEntity>> UpsertDocumentAsync(TEntity item, bool checkEtag = false, string idOverride = null)
        {
            if (item is IValidateable && !item.IsDraft)
            {
                var validationResult = Validator.Validate(item as IValidateable, Actions.Update);

                if (!validationResult.Successful)
                {
                    foreach (var error in validationResult.Errors)
                        _logger.AddCustomEvent(LogLevel.Error, $"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync]", $"Validation Error: {error.Message}", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("id", item.Id));

                    throw new ValidationException("Found invalid data at storage", validationResult.Errors);
                }
            }

            EntityDocumentStoragePolicy.ValidateForWrite(item);

            string eTag = null;
            if (checkEtag)
            {
                if (String.IsNullOrEmpty(item.ETag))
                    throw new ContentModifiedException { EntityType = typeof(TEntity).Name, Id = item.Id };

                eTag = item.ETag;
            }

            item.Revision++;
            item.RevisionTimeStamp = DateTime.UtcNow.ToJSONString();
            item.DatabaseName = _dbName;
            item.EntityType = typeof(TEntity).Name;

            var documentId = idOverride ?? item.Id;
            var ownerOrgId = item.OwnerOrganization?.Id;
            var sw = Stopwatch.StartNew();

            DependentObjectCheckResult dependencyResult = null;
            var nameChanged = false;

            if (_dependencyManager != null)
            {
                var existing = await GetDocumentAsync(documentId);

                nameChanged = !String.Equals(existing.Name, item.Name, StringComparison.Ordinal);

                if (nameChanged)
                {
                    dependencyResult = await _dependencyManager.CheckForDependenciesAsync(item);

                    if (item.AuditHistory == null)
                        item.AuditHistory = new List<EntityChangeSet>();

                    item.AuditHistory.Add(new EntityChangeSet
                    {
                        ChangeDate = DateTime.UtcNow.ToJSONString(),
                        ChangedBy = item.LastUpdatedBy,
                        Changes = new List<EntityChange>
                        {
                            new EntityChange { OldValue = existing.Name, NewValue = item.Name, Field = nameof(item.Name) }
                        }
                    });
                }
                else if (_verboseLogging)
                {
                    _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] - Object {item.Name} name not changed");
                }
            }
            else if (_verboseLogging)
            {
                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] - Dependency Manager is null");
            }

            if (item is IDiscussableEntity discussable)
            {
                Console.WriteLine("===================> Checking it is discussable <========================================");
                await PostDiscussionUpdates(discussable);
            }

            item.SetHash();

            var upsertResult = await _storageClient.UpsertDocumentAsync(item, eTag).ConfigureAwait(false);
            if (upsertResult?.Resource != null && !String.IsNullOrWhiteSpace(upsertResult.Resource.ETag))
                item.ETag = upsertResult.Resource.ETag;

            _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] Document Update {typeof(TEntity).Name} in {sw.Elapsed.TotalMilliseconds}ms");

            if (nameChanged && _dependencyManager != null)
            {
                if (dependencyResult?.IsInUse == true)
                {
                    _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] - Object {item.Name} has {dependencyResult.DependentObjects.Count} legacy dependencies.");

                    foreach (var dependentObject in dependencyResult.DependentObjects)
                        await _dependencyManager.RenameDependentObjectsAsync(item.LastUpdatedBy, documentId, typeof(TEntity).Name, dependentObject.Id, dependentObject.RecordType, item.Name);
                }

                if (!String.IsNullOrWhiteSpace(ownerOrgId))
                    await _dependencyManager.RenameRegisteredReferencesAsync(item.LastUpdatedBy, typeof(TEntity), documentId, ownerOrgId, item.Name);
                else
                    _logger.AddCustomEvent(LogLevel.Warning, $"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync]", $"Could not update registered references for renamed entity '{documentId}' because OwnerOrganization.Id was missing.");

                await _dependencyManager.RenameObjectAsync(item.LastUpdatedBy, documentId, typeof(TEntity).Name, item.Name);
            }

            if (_cacheProvider != null && (_cacheAborter == null || _cacheAborter != null && !_cacheAborter.AbortCache))
            {
                await _cacheProvider.RemoveAsync(GetCacheKey(documentId));
                await _cacheProvider.AddAsync(GetCacheKey(documentId), JsonConvert.SerializeObject(item));

                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__UpsertDocumentAsync] Added {typeof(TEntity).Name} back to cache after update in {sw.Elapsed.TotalMilliseconds}ms");
            }

            if (_ragIndexingServices != null && item.ShouldVectorIndex)
                await _ragIndexingServices.IndexAsync(item);

            if (_producedArtifactService != null)
                await _producedArtifactService.CreateProducedArtifactsAsync(item);

            await InvalidateEntityListCacheAsync(ownerOrgId);

            return upsertResult;
        }

        protected async Task<TEntity> GetDocumentAsync(string id, bool throwOnNotFound = true)
        {
            var sw = Stopwatch.StartNew();

            if (_cacheProvider != null && (_cacheAborter == null || !_cacheAborter.AbortCache))
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
                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync] Skip cache attempt Provider Is Null: {_cacheProvider != null}, AbortCache Flag: {_cacheAborter?.AbortCache} {GetCacheKey(id)}");
                DocumentNotCached.WithLabels(typeof(TEntity).Name).Inc();
            }

            sw = Stopwatch.StartNew();
            var doc = await GetDocumentAsync(id, null, throwOnNotFound);
            if (_cacheProvider != null && doc != null)
            {
                sw = Stopwatch.StartNew();
                await _cacheProvider.AddAsync(GetCacheKey(id), JsonConvert.SerializeObject(doc));
                _logger.Trace($"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync] Added To Cache {typeof(TEntity).Name} {GetCacheKey(id)} - {sw.ElapsedMilliseconds}ms");
            }

            return doc;
        }

        protected async Task<TEntity> GetDocumentAsync(string id, string partitionKey, bool throwOnNotFound = true)
        {
            var sw = Stopwatch.StartNew();
            using var timer = DocumentGet.WithLabels(typeof(TEntity).Name).NewTimer();

            try
            {
                var entity = await _storageClient.GetDocumentAsync<TEntity>(id, partitionKey, throwOnNotFound).ConfigureAwait(false);

                if (entity == null)
                {
                    DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                    return null;
                }

                if (!String.IsNullOrWhiteSpace(entity.EntityType) && entity.EntityType != typeof(TEntity).Name)
                {
                    DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                    if (throwOnNotFound) throw new RecordNotFoundException(typeof(TEntity).Name, id);
                    return null;
                }

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__GetDocumentAsync]", $"Load document [{entity.Name}], Org: {entity.OwnerOrganization?.Text} from storage in {sw.Elapsed.TotalMilliseconds}ms",
                    sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"), id.ToKVP("recordId"), entity.Name.ToKVP("entityName"));

                return entity;
            }
            catch (RecordNotFoundException)
            {
                DocumentNotFound.WithLabels(typeof(TEntity).Name).Inc();
                if (throwOnNotFound) throw;
                return null;
            }
        }

        protected async Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id, bool softDelete = true)
        {
            var sw = Stopwatch.StartNew();

            var timer = DocumentDelete.WithLabels(typeof(TEntity).Name).NewTimer();
            var doc = await GetDocumentAsync(id);

            if (_dependencyManager != null)
            {
                var legacyResult = await _dependencyManager.CheckForDependenciesAsync(doc);
                var registeredResult = await _dependencyManager.CheckRegisteredReferencesAsync(typeof(TEntity), doc.Id, doc.OwnerOrganization.Id, CancellationToken.None);

                var dependentObjects = legacyResult.DependentObjects
                    .Concat(registeredResult.DependentObjects)
                    .GroupBy(record => $"{record.RecordType}:{record.Id}", StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToArray();

                if (dependentObjects.Any())
                {
                    timer.Dispose();
                    throw new InUseException(DependentObjectCheckResult.InUse(dependentObjects.ToArray()));
                }
            }

            if (_cacheProvider != null)
            {
                var cacheKey = GetCacheKey(id);
                await _cacheProvider.RemoveAsync(cacheKey);
            }

            OperationResponse<TEntity> result;

            if (!softDelete || (doc.IsDeleted.HasValue && doc.IsDeleted.Value))
            {
                result = await _storageClient.DeleteDocumentAsync<TEntity>(doc.Id, doc.OwnerOrganization?.Id).ConfigureAwait(false);
                if (_ragIndexingServices != null)
                {
                    if (!EntityHeader.IsNullOrEmpty(doc.OwnerOrganization))
                        await _ragIndexingServices.RemoveIndexAsync(doc.OwnerOrganization.Id, doc.Id);
                }
            }
            else
            {
                doc.IsDeleted = true;
                doc.DeletionDate = UtcTimestamp.Now;
                result = await _storageClient.UpsertDocumentAsync(doc).ConfigureAwait(false);
                if (_ragIndexingServices != null && doc.ShouldVectorIndex)
                    await _ragIndexingServices.IndexAsync(doc);
            }
            timer.Dispose();

            _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__DeleteDocumentAsync]", $"Deleted Document {id} in {sw.Elapsed.TotalMilliseconds} ms",
                new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("recordId", id));

            await InvalidateEntityListCacheAsync(doc.OwnerOrganization?.Id);

            return result;
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

            doc.IsDeleted = true;
            doc.DeletionDate = UtcTimestamp.Now;
            var result = await _storageClient.UpsertDocumentAsync(doc).ConfigureAwait(false);
            timer.Dispose();

            _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__DeleteDocumentAsync]", $"Deleted Document {id}, partition key {partitionKey} in {sw.Elapsed.TotalMilliseconds} ms",
                new KeyValuePair<string, string>("Record Type", typeof(TEntity).Name), new KeyValuePair<string, string>("recordId", id));

            await InvalidateEntityListCacheAsync(doc.OwnerOrganization?.Id);

            return result;
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

        private async Task<IEnumerable<TEntity>> QueryAsync(string sql, params QueryParameter[] sqlParams)
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
                                       (!itm.IsDraft.IsDefined() || itm.IsDraft == false || listRequest.ShowDrafts))
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
                                         && (!itm.IsDraft.IsDefined() || itm.IsDraft == false || listRequest.ShowDrafts))
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

                if (listRequest.OrderBy != null && listRequest.OrderByDesc != null)
                {
                    return ListResponse<TEntitySummary>.FromError("order by AND order by desc were both provided, must either be both empty or only provide one of the two.");
                }

                if (listRequest.OrderBy != null)
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
                if (listRequest.ShowDrafts)
                    isDraftQuery = qry => true;

                System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> categoryQuery = (qry) => qry.Category.Key == listRequest.CategoryKey; ;
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

                if (orderByDesc != null)
                    linqQuery = linqQuery.OrderByDescending(orderByDesc);
                else if (sort != null)
                    linqQuery = linqQuery.OrderBy(sort);

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
                        .Where(itm => itm.EntityType == typeof(TEntity).Name && (itm.IsDeleted.IsNull() || !itm.IsDeleted.HasValue || !itm.IsDeleted.Value || listRequest.ShowDeleted) && (!itm.IsDraft.IsDefined() || itm.IsDraft == false || listRequest.ShowDrafts))
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

        private async Task<ListResponse<TEntity>> QueryAsync(string sql, ListRequest listRequest, params QueryParameter[] sqlParams)
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

        private async Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TEntitySummary : class, ISummaryData
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
                        .Where(itm => itm.EntityType == typeof(TEntity).Name && (!itm.IsDeleted.HasValue || !itm.IsDeleted.Value || listRequest.ShowDeleted) && (!itm.IsDraft.IsDefined() || itm.IsDraft == false || listRequest.ShowDrafts))
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

        private async Task<ListResponse<TMiscEntity>> QueryAsync<TMiscEntity>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TMiscEntity : class
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

        private async Task<List<TMiscEntity>> QueryAsync<TMiscEntity>(string sql, params QueryParameter[] sqlParams) where TMiscEntity : class
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

        protected bool Verbose
        {
            get => _verboseLogging;
            set => _verboseLogging = value;
        }
    }
}
