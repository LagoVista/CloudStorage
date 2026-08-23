using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Small mutable durable-cache / scratch storage capability.
    /// Kept distinct from application-data storage even when both are backed by MongoDB.
    /// </summary>
    public interface IScratchStore<TRecord>
        where TRecord : IScratchDataRecord
    {
        Task<TRecord> GetAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task UpsertAsync(TRecord record, CancellationToken cancellationToken = default);
        Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TRecord>> QueryAsync(StorageQuery<TRecord> query, CancellationToken cancellationToken = default);
    }
}
