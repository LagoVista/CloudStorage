using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class EntityPreparationCandidateRepository : IEntityPreparationCandidateRepository
    {
        private readonly Container _container;
        private readonly ILogger _logger;

        public EntityPreparationCandidateRepository(ISyncConnectionSettings options, ILogger logger)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var client = new CosmosClient(options.SyncConnectionSettings.Uri, options.SyncConnectionSettings.AccessKey, new CosmosClientOptions());
            _container = client.GetContainer(options.SyncConnectionSettings.ResourceName, $"{options.SyncConnectionSettings.ResourceName}_Collections");
        }

        public async Task<List<EntityBaseSummary>> GetIncompleteEntityBasesAsync(string entityType, string orgId, int maxItems, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("entityType is required.", nameof(entityType));

            if (String.IsNullOrWhiteSpace(orgId))
                throw new ArgumentException("orgId is required.", nameof(orgId));

            if (maxItems <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be greater than zero.");

            var take = Math.Min(maxItems, 5000);
            var sql = $@"SELECT TOP {take}
    c.id AS Id,
    c.EntityType AS EntityType,
    c.Name AS Name,
    c.Key AS Key,
    c.Description AS Description,
    c.Icon AS Icon,
    c.Category AS Category,
    c.IsDraft AS IsDraft,
    c.IsDeprecated AS IsDeprecated,
    c.MasterStatus AS MasterStatus,
    c.ReadinessStatus AS ReadinessStatus,
    c.CreationDate AS CreationDate,
    c.LastUpdatedDate AS LastUpdatedDate,
    c.Revision AS Revision
FROM c
WHERE c.EntityType = @entityType
AND c.OwnerOrganization.Id = @orgId
AND (
    NOT IS_DEFINED(c.MasterStatus)
    OR IS_NULL(c.MasterStatus)
    OR NOT IS_DEFINED(c.MasterStatus.IsProductionReady)
    OR IS_NULL(c.MasterStatus.IsProductionReady)
    OR c.MasterStatus.IsProductionReady != true
)
ORDER BY c.Name ASC";

            var query = new QueryDefinition(sql)
                .WithParameter("@entityType", entityType.Trim())
                .WithParameter("@orgId", orgId.Trim());

            var results = new List<EntityBaseSummary>();
            var requestOptions = new QueryRequestOptions { MaxItemCount = Math.Min(take, 100) };

            try
            {
                using var iterator = _container.GetItemQueryIterator<EntityBaseSummary>(query, requestOptions: requestOptions);

                while (iterator.HasMoreResults && results.Count < take)
                {
                    var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);

                    foreach (var entity in page.Resource.Where(item => item != null))
                    {
                        results.Add(entity);

                        if (results.Count >= take)
                            break;
                    }
                }

                _logger.Trace($"{this.Tag()} - Found {results.Count} incomplete entities of type '{entityType}' for organization '{orgId}'.");
                return results;
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
        }
    }
}
