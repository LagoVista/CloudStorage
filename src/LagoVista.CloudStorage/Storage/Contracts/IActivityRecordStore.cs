using LagoVista.Core.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Append-only storage for immutable activity records. Each TRecord maps to
    /// its own physical provider table/collection. Update/delete semantics are
    /// intentionally absent.
    /// </summary>
    public interface IActivityRecordStore<TRecord>
        where TRecord : IActivityRecord
    {
        Task InsertAsync(TRecord record, CancellationToken cancellationToken = default);
        Task InsertBatchAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TRecord>> QueryAsync(HistoryQuery<TRecord> query, CancellationToken cancellationToken = default);
    }
}
