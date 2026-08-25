using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Durable mutable application-data capability with indexed querying.
    /// Record type is supplied per operation so repositories compose one shared
    /// storage capability rather than requiring a closed generic DI registration.
    /// </summary>
    public interface IApplicationDataStore
    {
        Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord;

        Task InsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord;

        Task UpdateAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord;

        Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord;

        Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord;
    }
}
