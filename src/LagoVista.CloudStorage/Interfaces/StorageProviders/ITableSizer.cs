using LagoVista.CloudStorage.Utils.TableSizer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ITableSizer
    {
        Task<IReadOnlyList<TableSampleStats>> RunAsync(
                int sampleSizePerTable = 500,
                int maxConcurrency = 6,
                CancellationToken ct = default);
    }
}
