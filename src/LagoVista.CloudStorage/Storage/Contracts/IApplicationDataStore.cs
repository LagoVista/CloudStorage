using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Durable mutable application-data capability with indexed querying.
    /// Insert and update remain explicit operations rather than being collapsed
    /// into scratch-store upsert semantics.
    /// </summary>
    public interface IApplicationDataStore<TEntity>
    {
        Task<TEntity> GetAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TEntity>> QueryAsync(StorageQuery<TEntity> query, CancellationToken cancellationToken = default);
    }
}
