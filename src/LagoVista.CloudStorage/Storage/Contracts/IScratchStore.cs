using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Small mutable durable-cache / scratch storage capability.
    /// Kept distinct from flat-document storage even when both are backed by MongoDB.
    /// </summary>
    public interface IScratchStore<TEntity>
    {
        Task<TEntity> GetAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TEntity>> QueryAsync(StorageQuery<TEntity> query, CancellationToken cancellationToken = default);
    }
}
