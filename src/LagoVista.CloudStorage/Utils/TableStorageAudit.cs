using Azure.Data.Tables;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Utils
{
    namespace TableSizer
    {
        public sealed class TableSampleStats
        {
            public string Table { get; set; }
            public int SampledEntities { get; set; }
            public long SampledBytes { get; set; }
            public double AvgEntityBytes { get; set; }
            public int MaxEntityBytes { get; set; }

            public string MaxPartitionKey { get; set; }
            public string MaxRowKey { get; set; }

            public long RowCount { get; set; }

            public DateTime LastWritten { get; set; }

            public override string ToString()
            {
                return string.Format(
                    "{0,-40} avg={1,8:0}B  max={2,8}B  sampled={3,5}  maxKey={4}/{5}",
                    Table,
                    AvgEntityBytes,
                    MaxEntityBytes,
                    SampledEntities,
                    MaxPartitionKey ?? "",
                    MaxRowKey ?? "");
            }
        }


        public class TableSizer : ITableSizer
        {

            private readonly IAdminLogger _logger;
            private readonly string _accountKey;
            private readonly string _accountName;

            public TableSizer(IDefaultConnectionSettings connectionSettings, IAdminLogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                _accountName = connectionSettings.DefaultTableStorageSettings.AccountId;
                _accountKey = connectionSettings.DefaultTableStorageSettings.AccessKey;

                if (String.IsNullOrEmpty(_accountName)) throw new ArgumentNullException(nameof(_accountName));
                if (String.IsNullOrEmpty(_accountKey)) throw new ArgumentNullException(nameof(_accountKey));
            }

            public async Task<IReadOnlyList<TableSampleStats>> RunAsync(
                int sampleSizePerTable = 500,
                int maxConcurrency = 6,
                CancellationToken ct = default)
            {
                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountName};AccountKey={_accountKey}";

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new ArgumentException("connectionString is required.", nameof(connectionString));

                if (sampleSizePerTable <= 0)
                    throw new ArgumentOutOfRangeException(nameof(sampleSizePerTable));

                if (maxConcurrency <= 0)
                    throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

                var service = new TableServiceClient(connectionString);

                var tableNames = new List<string>();
                await foreach (var t in service.QueryAsync(cancellationToken: ct))
                {
                    if (!string.IsNullOrWhiteSpace(t.Name))
                        tableNames.Add(t.Name);
                }

                _logger.Trace($"{this.Tag()} - Found {tableNames.Count} tables. Starting sampling with max concurrency {maxConcurrency} and sample size {sampleSizePerTable} per table.");
                var idx = 1;
                var results = new ConcurrentBag<TableSampleStats>();
                using (var throttler = new SemaphoreSlim(maxConcurrency))
                {
                    var tasks = tableNames.Select(async tableName =>
                    {
                        await throttler.WaitAsync(ct).ConfigureAwait(false);
                        try
                        {
                            var thisIdx = idx++;
                            _logger.Trace($"{this.Tag()} - {thisIdx} - Start {tableName}");
                            var sw = Stopwatch.StartNew();
                            var tableClient = service.GetTableClient(tableName);

                            var stats = await SampleTableAsync(tableClient, tableName, sampleSizePerTable, ct).ConfigureAwait(false);

                            //stats.RowCount = await CountEntitiesExactAsync(tableClient, ct); 

                            _logger.Trace($"{this.Tag()} - {thisIdx} - End {tableName} found {stats.SampledEntities} in {sw.Elapsed.TotalSeconds} seconds");

                            results.Add(stats);
                        }
                        catch (Exception ex)
                        {
                            // keep going; optionally collect errors
                            Console.Error.WriteLine("[WARN] " + tableName + ": " + ex.Message);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }).ToArray();

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }

                return results
                    .OrderByDescending(r => r.AvgEntityBytes)
                    .ThenByDescending(r => r.MaxEntityBytes)
                    .ToList();
            }

            private static async Task<TableSampleStats> SampleTableAsync(
                TableClient tableClient,
                string tableName,
                int sampleSize,
                CancellationToken ct)
            {
                var stats = new TableSampleStats
                {
                    Table = tableName,
                    SampledEntities = 0,
                    SampledBytes = 0,
                    AvgEntityBytes = 0,
                    MaxEntityBytes = 0
                };

                // For sampling, we read full entities. If you already know which columns can be huge,
                // you can set "select" to only keys + those columns to reduce payload.
                await foreach (var entity in tableClient.QueryAsync<TableEntity>(maxPerPage: 200, cancellationToken: ct))
                {
                    var est = EstimateEntityBytes(entity);

                    stats.SampledBytes += est;
                    stats.SampledEntities++;

                    if (est > stats.MaxEntityBytes)
                    {
                        stats.MaxEntityBytes = est;
                        stats.MaxPartitionKey = entity.PartitionKey;
                        stats.MaxRowKey = entity.RowKey;
                    }

                    if (stats.SampledEntities >= sampleSize)
                        break;
                }

                stats.AvgEntityBytes = stats.SampledEntities == 0
                    ? 0
                    : (double)stats.SampledBytes / stats.SampledEntities;

                return stats;
            }

            private static async Task<long> CountEntitiesExactAsync(TableClient tableClient, CancellationToken ct)
            {
                long count = 0;
                var select = new[] { "PartitionKey", "RowKey" };

                await foreach (var _ in tableClient.QueryAsync<TableEntity>(select: select, maxPerPage: 1000, cancellationToken: ct))
                {
                    ct.ThrowIfCancellationRequested();
                    count++;
                }

                return count;
            }

            // Heuristic size estimator: good for ranking. Not an exact billing byte count.
            private static int EstimateEntityBytes(TableEntity e)
            {
                var size = 0;

                // Keys
                if (!string.IsNullOrEmpty(e.PartitionKey))
                    size += Encoding.UTF8.GetByteCount(e.PartitionKey);

                if (!string.IsNullOrEmpty(e.RowKey))
                    size += Encoding.UTF8.GetByteCount(e.RowKey);

                foreach (var kvp in e)
                {
                    // property name overhead (rough)
                    if (!string.IsNullOrEmpty(kvp.Key))
                        size += kvp.Key.Length;

                    var v = kvp.Value;
                    if (v == null) continue;

                    if (v is string s)
                    {
                        size += Encoding.UTF8.GetByteCount(s);
                    }
                    else if (v is byte[] b)
                    {
                        size += b.Length;
                    }
                    else if (v is bool)
                    {
                        size += 1;
                    }
                    else if (v is int || v is float)
                    {
                        size += 4;
                    }
                    else if (v is long || v is double || v is DateTime || v is DateTimeOffset)
                    {
                        size += 8;
                    }
                    else if (v is Guid)
                    {
                        size += 16;
                    }
                    else if (v is decimal)
                    {
                        size += 16;
                    }
                    else
                    {
                        // fallback
                        var str = v.ToString() ?? string.Empty;
                        size += Encoding.UTF8.GetByteCount(str);
                    }
                }

                return size;
            }
        }
    }
}