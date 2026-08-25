using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Small mutable durable-cache / scratch storage capability.
    /// Record type is supplied per operation so repositories compose one shared
    /// storage capability rather than requiring a closed generic DI registration.
    /// </summary>
    public interface IScratchStore
    {
        Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : IScratchDataRecord;

        Task UpsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
            where TRecord : IScratchDataRecord;

        Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : IScratchDataRecord;

        Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken = default)
            where TRecord : IScratchDataRecord;
    }
}
