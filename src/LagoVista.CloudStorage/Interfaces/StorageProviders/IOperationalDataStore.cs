using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Mutable storage for small standalone operational records. Each TRecord maps
    /// to its own physical provider table/collection and is addressed by organization
    /// scope plus record Id.
    /// </summary>
    public interface IOperationalDataStore<TRecord>
        where TRecord : class, IOperationalDataRecord
    {
        Task<TRecord> GetAsync(string organizationId, string id, CancellationToken cancellationToken = default);
        Task UpsertAsync(TRecord record, CancellationToken cancellationToken = default);
        Task UpsertBatchAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default);
        Task DeleteAsync(string organizationId, string id, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TRecord>> QueryAsync(StorageQuery<TRecord> query, CancellationToken cancellationToken = default);
    }
}
