using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Strangler implementation for EntityUtilsRepository. Provider-neutral operations move
    /// here a coherent slice at a time while the inherited Cosmos implementation remains the
    /// compatibility path for behavior that has not yet been converted.
    /// </summary>
    public sealed class ProviderNeutralEntityUtilsRepository : EntityUtilsRepository, IEntityUtilsRepository
    {
        private readonly DocumentStorageSettings _storageSettings;
        private readonly IDocumentCollectionFactory _collectionFactory;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;

        public ProviderNeutralEntityUtilsRepository(
            ISyncConnectionSettings options,
            ICosmosClientProvider cosmosClientProvider,
            IEntityDetailResponseFactory entityDetailResponseFactory,
            IDependencyManager dependencyManager,
            ICacheProvider cacheProvider,
            IAdminLogger logger,
            IRagIndexingServices ragIndexingServices,
            IEntityListCacheInvalidator entityListCacheInvalidator)
            : base(options, cosmosClientProvider, entityDetailResponseFactory, dependencyManager, cacheProvider, logger, ragIndexingServices, entityListCacheInvalidator)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (cosmosClientProvider == null) throw new ArgumentNullException(nameof(cosmosClientProvider));

            _collectionNameResolver = new DocumentCollectionNameResolver();
            _collectionFactory = new DocumentCollectionFactory(cosmosClientProvider, _collectionNameResolver);
            _storageSettings = DocumentStorageSettingsResolver.Resolve(
                options.SyncConnectionSettings.Uri,
                options.SyncConnectionSettings.AccessKey,
                options.SyncConnectionSettings.ResourceName);
        }

        public new async Task<InvokeResult<List<JObject>>> GetEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct)
        {
            try
            {
                ValidateTypeAndOrg(entityType, orgId);
                if (!TryGetCollection(entityType, out var collection))
                    return InvokeResult<List<JObject>>.Create(new List<JObject>());

                var request = new DocumentQueryRequest(DocumentQueryType.EntityUtilsDocumentsByType)
                    .WithParameter("entityType", entityType.Trim())
                    .WithParameter("orgId", orgId.Trim());

                var documents = (await collection.QueryAsync<JObject>(request, ct).ConfigureAwait(false)).ToList();
                return InvokeResult<List<JObject>>.Create(documents);
            }
            catch (Exception ex)
            {
                return InvokeResult<List<JObject>>.FromException(nameof(GetEntitiesByTypeAsync), ex);
            }
        }

        public new async Task<List<EntityCoreSummary>> GetEntityCoreAsync(string entityType, EntityHeader org, CancellationToken ct = default)
        {
            ValidateOrg(org);
            var result = await GetEntitiesByTypeAsync(entityType, org.Id, ct).ConfigureAwait(false);
            if (!result.Successful)
                throw new InvalidOperationException($"Could not retrieve entities of type '{entityType}'.");

            return (result.Result ?? new List<JObject>())
                .Select(document => document.ToObject<EntityCoreSummary>())
                .Where(entity => entity != null)
                .ToList();
        }

        public new async Task<List<EntityBaseSummary>> GetEntityBasesAsync(string entityType, EntityHeader org, CancellationToken ct = default)
        {
            ValidateOrg(org);
            var result = await GetEntitiesByTypeAsync(entityType, org.Id, ct).ConfigureAwait(false);
            if (!result.Successful)
                throw new InvalidOperationException($"Could not retrieve entities of type '{entityType}'.");

            return (result.Result ?? new List<JObject>())
                .Select(document => document.ToObject<EntityBaseSummary>())
                .Where(entity => entity != null)
                .ToList();
        }

        public new async Task<JObject> GetEntityByIdAsync(string entityType, string entityId, string orgId, CancellationToken token)
        {
            ValidateTypeAndOrg(entityType, orgId);
            if (String.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Entity id is required.", nameof(entityId));
            if (!TryGetCollection(entityType, out var collection))
                return null;

            var request = new DocumentQueryRequest(DocumentQueryType.EntityUtilsDocumentById)
                .WithParameter("entityType", entityType.Trim())
                .WithParameter("entityId", entityId.Trim())
                .WithParameter("orgId", orgId.Trim());

            return (await collection.QueryAsync<JObject>(request, token).ConfigureAwait(false)).FirstOrDefault();
        }

        public new async Task<InvokeResult<int>> CountEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct)
        {
            try
            {
                ValidateTypeAndOrg(entityType, orgId);
                if (!TryGetCollection(entityType, out var collection))
                    return InvokeResult<int>.Create(0);

                var request = new DocumentQueryRequest(DocumentQueryType.EntityUtilsCountByType)
                    .WithParameter("entityType", entityType.Trim())
                    .WithParameter("orgId", orgId.Trim());

                var result = (await collection.QueryAsync<DocumentCountResult>(request, ct).ConfigureAwait(false)).FirstOrDefault();
                return InvokeResult<int>.Create(result?.Count ?? 0);
            }
            catch (Exception ex)
            {
                return InvokeResult<int>.FromException(nameof(CountEntitiesByTypeAsync), ex);
            }
        }

        Task<List<EntityCoreSummary>> IEntityUtilsRepository.GetEntityCoreAsync(string entityType, EntityHeader org, CancellationToken ct) => GetEntityCoreAsync(entityType, org, ct);
        Task<List<EntityBaseSummary>> IEntityUtilsRepository.GetEntityBasesAsync(string entityType, EntityHeader org, CancellationToken ct) => GetEntityBasesAsync(entityType, org, ct);
        Task<InvokeResult<List<JObject>>> IEntityUtilsRepository.GetEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct) => GetEntitiesByTypeAsync(entityType, orgId, ct);
        Task<JObject> IEntityUtilsRepository.GetEntityByIdAsync(string entityType, string entityId, string orgId, CancellationToken token) => GetEntityByIdAsync(entityType, entityId, orgId, token);
        Task<InvokeResult<int>> IEntityUtilsRepository.CountEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct) => CountEntitiesByTypeAsync(entityType, orgId, ct);

        private bool TryGetCollection(string entityType, out IDocumentCollection collection)
        {
            if (_storageSettings.Provider == DocumentStorageProviderType.Cosmos)
            {
                collection = _collectionFactory.Create(_storageSettings, $"{_storageSettings.DatabaseName}_Collections");
                return true;
            }

            var mongoDatabaseName = _storageSettings.Mongo?.DatabaseName ?? _storageSettings.DatabaseName;
            if (!_collectionNameResolver.TryResolve(mongoDatabaseName, entityType, out var collectionName))
            {
                collection = null;
                return false;
            }

            collection = _collectionFactory.Create(_storageSettings, collectionName);
            return true;
        }

        private static void ValidateTypeAndOrg(string entityType, string orgId)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("Organization id is required.", nameof(orgId));
        }

        private static void ValidateOrg(EntityHeader org)
        {
            if (org == null) throw new ArgumentNullException(nameof(org));
            if (String.IsNullOrWhiteSpace(org.Id)) throw new ArgumentException("org.Id is required.", nameof(org));
        }
    }
}
