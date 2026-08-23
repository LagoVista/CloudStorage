using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class EntityPreparationCandidateRepository : IEntityPreparationCandidateRepository
    {
        private readonly DocumentStorageSettings _storageSettings;
        private readonly IDocumentCollectionFactory _collectionFactory;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;
        private readonly ILogger _logger;

        public EntityPreparationCandidateRepository(ISyncConnectionSettings options, ICosmosClientProvider cosmosClientProvider, ILogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (cosmosClientProvider == null) throw new ArgumentNullException(nameof(cosmosClientProvider));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _collectionNameResolver = new DocumentCollectionNameResolver();
            _collectionFactory = new DocumentCollectionFactory(cosmosClientProvider, _collectionNameResolver);
            _storageSettings = DocumentStorageSettingsResolver.Resolve(options.SyncConnectionSettings.Uri, options.SyncConnectionSettings.AccessKey, options.SyncConnectionSettings.ResourceName);
        }

        public async Task<EntityBaseSummary> GetEntityBaseAsync(string entityType, string entityId, string orgId, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("entityType is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("entityId is required.", nameof(entityId));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("orgId is required.", nameof(orgId));

            try
            {
                var request = new DocumentQueryRequest(DocumentQueryType.EntityPreparationCandidateById)
                    .WithParameter("entityType", entityType.Trim())
                    .WithParameter("entityId", entityId.Trim())
                    .WithParameter("orgId", orgId.Trim());

                var entities = await GetCollection(entityType).QueryAsync<EntityBaseSummary>(request, ct).ConfigureAwait(false);
                return entities.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
        }

        public async Task<List<EntityBaseSummary>> GetEntityBasesAsync(string entityType, string orgId, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("entityType is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("orgId is required.", nameof(orgId));

            try
            {
                var request = new DocumentQueryRequest(DocumentQueryType.EntityPreparationCandidatesByType)
                    .WithParameter("entityType", entityType.Trim())
                    .WithParameter("orgId", orgId.Trim());

                var entities = (await GetCollection(entityType).QueryAsync<EntityBaseSummary>(request, ct).ConfigureAwait(false)).ToList();
                _logger.Trace($"{this.Tag()} - Found {entities.Count} entities of type '{entityType}' for organization '{orgId}'.");
                return entities;
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
        }

        public async Task<List<EntityBaseSummary>> GetIncompleteEntityBasesAsync(string entityType, string orgId, int maxItems, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("entityType is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("orgId is required.", nameof(orgId));
            if (maxItems <= 0) throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be greater than zero.");

            try
            {
                var request = CreateIncompleteRequest(entityType, orgId, maxItems);
                var entities = (await GetCollection(entityType).QueryAsync<EntityBaseSummary>(request, ct).ConfigureAwait(false)).ToList();
                _logger.Trace($"{this.Tag()} - Found {entities.Count} incomplete entities of type '{entityType}' for organization '{orgId}'.");
                return entities;
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
        }

        public async Task<ListResponse<EntityBaseSummary>> GetAllEntitiesByTypeAsync(string entityType, ListRequest listRequest, EntityHeader user, EntityHeader org, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("entityType is required.", nameof(entityType));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));
            if (org == null) throw new ArgumentNullException(nameof(org));
            if (String.IsNullOrWhiteSpace(org.Id)) throw new ArgumentException("org.Id is required.", nameof(org));

            try
            {
                var request = CreateIncompleteRequest(entityType, org.Id, listRequest.PageSize);
                var entities = (await GetCollection(entityType).QueryAsync<EntityBaseSummary>(request, ct).ConfigureAwait(false)).ToList();
                _logger.Trace($"{this.Tag()} - Found {entities.Count} incomplete entities of type '{entityType}' for organization '{org.Text}'.");
                return ListResponse<EntityBaseSummary>.Create(entities);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
        }

        private IDocumentCollection GetCollection(string entityType)
        {
            if (_storageSettings.Provider == DocumentStorageProviderType.Cosmos)
                return _collectionFactory.Create(_storageSettings, $"{_storageSettings.DatabaseName}_Collections");

            var mongoDatabaseName = _storageSettings.Mongo?.DatabaseName ?? _storageSettings.DatabaseName;
            if (!_collectionNameResolver.TryResolve(mongoDatabaseName, entityType, out var collectionName))
                throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            return _collectionFactory.Create(_storageSettings, collectionName);
        }

        private static DocumentQueryRequest CreateIncompleteRequest(string entityType, string orgId, int maxItems)
        {
            return new DocumentQueryRequest(DocumentQueryType.IncompleteEntityPreparationCandidatesByType)
                .WithParameter("entityType", entityType.Trim())
                .WithParameter("orgId", orgId.Trim())
                .WithParameter("maxItems", Math.Min(maxItems, 5000));
        }
    }
}
