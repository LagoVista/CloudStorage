using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityPreparationCandidateRepository
    {
        Task<EntityBaseSummary> GetEntityBaseAsync(string entityType, string entityId, string orgId, CancellationToken ct = default);

        Task<List<EntityBaseSummary>> GetEntityBasesAsync(string entityType, string orgId, CancellationToken ct = default);

        Task<List<EntityBaseSummary>> GetIncompleteEntityBasesAsync(string entityType, string orgId, int maxItems, CancellationToken ct = default);
        Task<ListResponse<EntityBaseSummary>> GetAllEntitiesByTypeAsync(string entityType, ListRequest listRequest, EntityHeader org, EntityHeader user, CancellationToken cancellation = default);
    }
}
