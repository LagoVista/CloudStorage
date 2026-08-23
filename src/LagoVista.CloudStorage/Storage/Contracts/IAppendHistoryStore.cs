using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Append-only, time-oriented storage capability. Implementations may expose
    /// retention, bucketing, and indexed filters through storage definition metadata,
    /// but mutation semantics are intentionally absent from this contract.
    /// </summary>
    public interface IAppendHistoryStore<TEntity>
    {
        Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task InsertBatchAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TEntity>> QueryAsync(HistoryQuery<TEntity> query, CancellationToken cancellationToken = default);
    }
}
