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
        Task<InvokeResult> PruneTableAsync(string tableName, CancellationToken ct);
    }
}
