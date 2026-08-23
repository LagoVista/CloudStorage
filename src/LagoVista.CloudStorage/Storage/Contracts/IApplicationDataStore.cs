using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Durable mutable application-data capability with indexed querying.
    /// Insert and update remain explicit operations rather than being collapsed
    /// into scratch-store upsert semantics.
    /// </summary>
    public interface IApplicationDataStore<TRecord>
        where TRecord : IApplicationDataRecord
    {
        Task<TRecord> GetAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task InsertAsync(TRecord record, CancellationToken cancellationToken = default);
        Task UpdateAsync(TRecord record, CancellationToken cancellationToken = default);
        Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TRecord>> QueryAsync(StorageQuery<TRecord> query, CancellationToken cancellationToken = default);
    }
}
