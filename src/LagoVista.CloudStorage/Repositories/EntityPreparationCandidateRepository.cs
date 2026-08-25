using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Repositories
{
    public sealed class EntityPreparationCandidateRepository : IEntityPreparationCandidateRepository
    {
        private readonly IDocumentStorageClient _storageClient;
        private readonly ILogger _logger;

        public EntityPreparationCandidateRepository(IDocumentStorageClientProvider documentStorageClientProvider, ILogger logger)
        {
            if (documentStorageClientProvider == null) throw new ArgumentNullException(nameof(documentStorageClientProvider));

            _storageClient = documentStorageClientProvider.GetClient();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                var entities = await _storageClient.QueryKnownAsync<EntityBaseSummary>(entityType, request, ct).ConfigureAwait(false);
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

                var entities = (await _storageClient.QueryKnownAsync<EntityBaseSummary>(entityType, request, ct).ConfigureAwait(false)).ToList();
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
                var entities = (await _storageClient.QueryKnownAsync<EntityBaseSummary>(entityType, request, ct).ConfigureAwait(false)).ToList();
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
                var entities = (await _storageClient.QueryKnownAsync<EntityBaseSummary>(entityType, request, ct).ConfigureAwait(false)).ToList();
                _logger.Trace($"{this.Tag()} - Found {entities.Count} incomplete entities of type '{entityType}' for organization '{org.Text}'.");
                return ListResponse<EntityBaseSummary>.Create(entities);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
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
