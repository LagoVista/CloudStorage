using LagoVista.CloudStorage.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ICassandraAdminRepo
    {
        string Keyspace { get; }
        Task<IReadOnlyList<CassandraTableInfo>> GetTablesAsync(CancellationToken cancellationToken = default);
        Task<CassandraQueryResult> QueryAsync(CassandraQueryRequest request, CancellationToken cancellationToken = default);
        Task ExecuteAsync(CassandraExecuteRequest request, CancellationToken cancellationToken = default);
    }
}
