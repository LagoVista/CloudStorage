using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IFkIndexTableWriterBatched
    {
        Task AddOrphanedEHAsync(IEntityBase entity, string path, EntityHeader eh, CancellationToken ct = default);
        Task ApplyDiffAsync(FkEdgeDiff.DiffResult diff, CancellationToken ct = default);
        Task UpsertAllAsync(IEnumerable<ForeignKeyEdge> edges, CancellationToken ct = default);
    }
}