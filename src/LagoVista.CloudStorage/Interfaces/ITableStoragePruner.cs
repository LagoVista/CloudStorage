using LagoVista.CloudStorage.Utils.TableSizer;
using LagoVista.Core.Validation;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ITableStoragePruner
    {
        Task<IReadOnlyList<TablePruningOperations>> RunAsync(PruneOptions pruneOptions, int sampleSizePerTable = 500, int maxConcurrency = 6, CancellationToken ct = default);
        Task<InvokeResult> DeleteTableAsync(string tableName, CancellationToken ct);

        Task<long> DeleteWhereAsync(string tableName, string filter, bool dryRun = false, int pageSize = 1000, CancellationToken ct = default);
    }
}
