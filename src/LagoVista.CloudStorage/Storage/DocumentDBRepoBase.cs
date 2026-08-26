// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 0966a614884bf62f14e46c78019b9280b027724296e08c242d0c640e6d6e7d98
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
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
using Newtonsoft.Json;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.DocumentDB
{
    public class DocumentDBRepoBase<TEntity> where TEntity : class, IEntityBase
    {
        private readonly string _dbName;
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

        // GUARD: Replace the complete private DocumentDBRepoBase(...) constructor.
        private DocumentDBRepoBase(IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null, IFkIndexTableWriterBatched fkWriter = null, IDocumentStorageClientProvider documentStorageClientProvider = null)
        {
            _logger = logger;
            _cacheProvider = cacheProvider;
            _dependencyManager = dependencyManager;
            _fkeyIndexWriter = fkWriter;

            if (documentStorageClientProvider == null) throw new ArgumentNullException(nameof(documentStorageClientProvider));

            _storageClient = documentStorageClientProvider.GetClient() ?? throw new InvalidOperationException("Document storage client provider returned null.");
            _dbName = _storageClient.DatabaseName;

            if (String.IsNullOrWhiteSpace(_dbName)) throw new InvalidOperationException("Document storage client returned an empty database name.");
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
            using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

            var items = await _storageClient.QueryAsync(query).ConfigureAwait(false);
            var result = items?.ToList() ?? new List<TEntity>();

            _logger.AddCustomEvent(LogLevel.Message,
                $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync]",
                $"Query returned {result.Count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms",
                typeof(TEntity).Name.ToKVP("recordType"),
                result.Count.ToString().ToKVP("recordCount"),
                sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

            return result;
        }

        protected async Task<ListResponse<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var listResponse = await _storageClient.QueryAsync(query, listRequest).ConfigureAwait(false);
                var count = listResponse?.Model?.Count() ?? 0;

                _logger.AddCustomEvent(LogLevel.Message,
                    $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync__ListRequest]",
                    $"Paged query returned {count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms",
                    typeof(TEntity).Name.ToKVP("recordType"),
                    count.ToString().ToKVP("recordCount"),
                    sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

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
                using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var listResponse = await _storageClient.QueryAsync(query, sort, listRequest).ConfigureAwait(false);
                var count = listResponse?.Model?.Count() ?? 0;

                _logger.AddCustomEvent(LogLevel.Message,
                    $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAsync__ListRequest__Sorted]",
                    $"Sorted paged query returned {count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms",
                    typeof(TEntity).Name.ToKVP("recordType"),
                    count.ToString().ToKVP("recordCount"),
                    sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

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

        protected async Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary, TEntityFactory>(Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory : class, ICategorized, ISummaryFactory, INoSQLEntity, INamedEntity, IRatedEntity, IAuditableEntity
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                if (listRequest.OrderBy != null && listRequest.OrderByDesc != null)
                    return ListResponse<TEntitySummary>.FromError("order by AND order by desc were both provided, must either be both empty or only provide one of the two.");

                var descending = false;

                if (listRequest.OrderBy != null)
                {
                    switch (listRequest.OrderBy.Value)
                    {
                        case OrderByTypes.Name:
                            sort = ele => ele.Name;
                            break;
                        case OrderByTypes.Rating:
                            sort = ele => ele.Stars.ToString();
                            break;
                        case OrderByTypes.CreationDate:
                            sort = ele => ele.CreationDate;
                            break;
                        case OrderByTypes.LastUpdateDate:
                            sort = ele => ele.LastUpdatedDate;
                            break;
                    }
                }

                if (listRequest.OrderByDesc != null)
                {
                    descending = true;

                    switch (listRequest.OrderByDesc.Value)
                    {
                        case OrderByTypes.Name:
                            sort = ele => ele.Name;
                            break;
                        case OrderByTypes.Rating:
                            sort = ele => ele.Stars.ToString();
                            break;
                        case OrderByTypes.CreationDate:
                            sort = ele => ele.CreationDate;
                            break;
                        case OrderByTypes.LastUpdateDate:
                            sort = ele => ele.LastUpdatedDate;
                            break;
                    }
                }

                var factoryResponse = await _storageClient.QuerySummaryAsync(typeof(TEntity).Name, query, sort, listRequest, descending).ConfigureAwait(false);
                var items = factoryResponse?.Model?.ToList() ?? new List<TEntityFactory>();

                var listResponse = ListResponse<TEntitySummary>.Create(listRequest, items.Select(item => item.CreateSummary() as TEntitySummary));
                var categories = listResponse.Model.Where(item => !String.IsNullOrEmpty(item.CategoryKey)).ToList();
                var groupedCategories = categories.Select(item => EnumDescription.Create(item.CategoryId, item.CategoryKey, item.Category)).GroupBy(item => item.Id);
                listResponse.Categories = groupedCategories.Select(item => item.First()).ToList();
                listResponse.Categories.Insert(0, EnumDescription.CreateSelect("-select category-"));

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryAsync]", $"Summary query returned {items.Count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms", items.Count.ToString().ToKVP("recordCount"), typeof(TEntity).Name.ToKVP("recordType"), sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

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

        // GUARD: Replace the complete QuerySummaryDescendingAsync<TEntitySummary, TEntityFactory> method.
        protected async Task<ListResponse<TEntitySummary>> QuerySummaryDescendingAsync<TEntitySummary, TEntityFactory>(Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory : class, ISummaryFactory, INoSQLEntity, ICategorized, IAuditableEntity
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var factoryResponse = await _storageClient.QuerySummaryAsync(typeof(TEntity).Name, query, sort, listRequest, true).ConfigureAwait(false);
                var items = factoryResponse?.Model?.ToList() ?? new List<TEntityFactory>();

                var listResponse = ListResponse<TEntitySummary>.Create(listRequest, items.Select(item => item.CreateSummary() as TEntitySummary));
                listResponse.Categories = listResponse.Model.Where(item => !String.IsNullOrEmpty(item.CategoryKey)).Select(item => EnumDescription.Create(item.CategoryId, item.CategoryKey, item.Category)).GroupBy(item => item.Id).Select(item => item.First()).ToList();

                if (listResponse.Categories.Any())
                    listResponse.Categories.Insert(0, EnumDescription.CreateSelect("-select category-"));

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QuerySummaryDescendingAsync]", $"Descending summary query returned {items.Count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms", items.Count.ToString().ToKVP("recordCount"), typeof(TEntity).Name.ToKVP("recordType"), sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

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


        protected async Task<ListResponse<TEntity>> QueryDescendingAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query,
                          System.Linq.Expressions.Expression<Func<TEntity, string>> sort, ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var listResponse = await _storageClient.QueryAsync(query, sort, listRequest, true).ConfigureAwait(false);
                var count = listResponse?.Model?.Count() ?? 0;

                _logger.AddCustomEvent(LogLevel.Message,
                    $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryDescendingAsync]",
                    $"Descending paged query returned {count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms",
                    typeof(TEntity).Name.ToKVP("recordType"),
                    count.ToString().ToKVP("recordCount"),
                    sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

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

    
        protected async Task<ListResponse<TEntity>> QueryAllAsync(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var listResponse = await _storageClient.QueryAllAsync(query, listRequest).ConfigureAwait(false);
                var count = listResponse?.Model?.Count() ?? 0;

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__QueryAllAsync]", $"Paged query returned {count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms", typeof(TEntity).Name.ToKVP("recordType"), count.ToString().ToKVP("recordCount"), sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

                return listResponse;
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

        // GUARD: Replace the complete DescOrderQueryAsync<TKey> method.
        protected async Task<ListResponse<TEntity>> DescOrderQueryAsync<TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> orderBy, ListRequest listRequest)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var timer = DocumentQuery.WithLabels(typeof(TEntity).Name).NewTimer();

                var listResponse = await _storageClient.QueryAllAsync(query, orderBy, listRequest, true).ConfigureAwait(false);
                var count = listResponse?.Model?.Count() ?? 0;

                _logger.AddCustomEvent(LogLevel.Message, $"[DocumentDBBase<{typeof(TEntity).Name}>__DescOrderQueryAsync<TKey>]", $"Descending query returned {count} {typeof(TEntity).Name} documents in {sw.Elapsed.TotalMilliseconds} ms", typeof(TEntity).Name.ToKVP("recordType"), count.ToString().ToKVP("recordCount"), sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"));

                return listResponse;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[DocumentDBBase<{typeof(TEntity).Name}>__DescOrderQueryAsync<TKey>]", ex, typeof(TEntity).Name.ToKVP("entityType"));

                DocumentErrors.WithLabels(typeof(TEntity).Name).Inc();

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
