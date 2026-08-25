using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public interface IMetricsStore
    {
        Task RegisterDefinitionAsync(MetricDefinition definition, CancellationToken cancellationToken = default);
        Task<MetricDefinition> GetDefinitionAsync(string metric, CancellationToken cancellationToken = default);
        Task RecordAsync(MetricRecord record, CancellationToken cancellationToken = default);
        Task RecordBatchAsync(IEnumerable<MetricRecord> records, CancellationToken cancellationToken = default);
        Task<MetricQueryResult> QueryAsync(MetricQuery query, CancellationToken cancellationToken = default);
    }
}
