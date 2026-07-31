using LagoVista.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityPreparationCandidateRepository
    {
        Task<List<EntityBaseSummary>> GetIncompleteEntityBasesAsync(string entityType, string orgId, int maxItems, CancellationToken ct = default);
    }
}
