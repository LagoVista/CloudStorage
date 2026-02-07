using Azure.Data.Tables;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using MongoDB.Driver.Core.Configuration;
using OpenTelemetry.Trace;
using System;
using System.ClientModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Utils
{
    namespace TableSizer
    {

        public class PruneOptions
        {
            public bool DryRun { get; set; } = true;
            public bool PruneEmptyTables { get; set; } = true;  
            public bool PruneOldTables { get; set; } = true;
            public bool PruneBasedOnLastWrite { get; set; } = true;
        }

        public class TablePruningOperations
        {
            public string TableName { get; set; }
            public bool WasDeleted { get; set; }
            public bool WouldDelete { get; set; }
            public string DeleteReason { get; set; }
        }


        public class TableStoragePruner : ITableStoragePruner
        {

            private readonly IAdminLogger _logger;
            private readonly string _accountKey;
            private readonly string _accountName;

            private IReadOnlyList<string> SystemTables = new List<string>()
            {
                "AgentTurnChatHistoryDto",
                "AppInstance",
                "AppUserInboxItem",
                "AuthenticationLog",
                "CommunicationsLog",
                "CompletedDeploymentActivity",
                "ComputeResourceMetrics",
                "ContactDTO",
                "ContactIntake",
                "CustomerFollowupDTO",
                "DeploymentActivity",
                "DeploymentHostStatus",
                "EmailUnsubscribeDTO",
                "DeploymentInstanceStatus",
                "DeviceNotificationHistory",
                "FailedDeploymentActivity",
                "FirmwareDownloadRequestDTO",
                "GeneratedReports",
                "GeneratedReportsDevices",
                "GuideCheckForUnderstandingAnswerDTO",
                "GuideCompletionStatusDTO",
                "Invitation",
                "InvoiceAuditTrailDTO",
                "InstanceAccountDTO",
                "Invitation",
                "jobs",
                "LabelSample",
                "MemoryNoteDTO",
                "NodeLocator",
                "OrganizationAccount",
                "OrganizationUserRole",
                "OrgUser",
                "PasskeyCredentialEntity",
                "PasskeyCredentialIndexEntity",
                "RaisedNotificationHistory",
                "RefreshToken",
                "ReportHistory",
                "ReportHistoryDTO",
                "Sample",
                "SampleLabel",
                "RoleAccessDTO",
                "SecureLink",
                "ShortendedLink",
                "singleusetoken",
                "sslcerts",
                "SolutionVersion",
                "sslcerts",
                "singleusetoken",
                "SurveyResponse",
                "SurveyResponseAnswer",
                "SurveyResult",
                "SurveyResultAnswer",
                "SystemNotification",
                "FkInbound",
                "FkOphaned",
                "FkOutbound",
                "UserRoleDTO",
                "WebSiteMetric",
                "WebSiteMetricAll",
                "WebSiteMetricByPath",
                "WebSiteMetricBySession",
            };


            public TableStoragePruner(IDefaultConnectionSettings connectionSettings, IAdminLogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                _accountName = connectionSettings.DefaultTableStorageSettings.AccountId;
                _accountKey = connectionSettings.DefaultTableStorageSettings.AccessKey;

                if (String.IsNullOrEmpty(_accountName)) throw new ArgumentNullException(nameof(_accountName));
                if (String.IsNullOrEmpty(_accountKey)) throw new ArgumentNullException(nameof(_accountKey));
            }

            public async Task<InvokeResult> PruneTableAsync(string tableName, CancellationToken ct = default)
            {
                try
                {
                    var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountName};AccountKey={_accountKey}";

                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new ArgumentException("connectionString is required.", nameof(connectionString));

                    var service = new TableServiceClient(connectionString);
                    var client = service.GetTableClient(tableName);
                    await client.DeleteAsync(ct);

                    return InvokeResult.Success;
                }
                catch (Exception ex)
                {
                    return InvokeResult.FromException(this.Tag(), ex);
                }
            }

            public async Task<IReadOnlyList<TablePruningOperations>> RunAsync(
                PruneOptions pruneOptions,
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
                var results = new ConcurrentBag<TablePruningOperations>();
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
                            if (!SystemTables.Contains(tableName) && !tableName.ToLower().StartsWith("datastream"))
                            {
                                if (tableName.StartsWith("m") && tableName.Length > 7 && pruneOptions.PruneOldTables)
                                {
                                    var uearPortion = tableName.Substring(1, 4);
                                    var monthPortion = tableName.Substring(5, 2);
                                    if (int.TryParse(uearPortion, out var year) && int.TryParse(monthPortion, out var month))
                                    {
                                        var tableDate = new DateTime(year, month, 1);
                                        var delta = DateTime.UtcNow - tableDate;
                                        if (delta.Days > 3 * 30)
                                        {
                                            results.Add(new TablePruningOperations()
                                            {
                                                TableName = tableName,
                                                WasDeleted = false,
                                                WouldDelete = true,
                                                DeleteReason = $"Table is {delta.Days} old"
                                            });
                                        }
                                        else
                                        {
                                            results.Add(new TablePruningOperations()
                                            {
                                                TableName = tableName,
                                                WasDeleted = false,
                                                WouldDelete = false,
                                                DeleteReason = $"None - table is current"
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    var stats = await SampleTableAsync(tableClient, tableName, sampleSizePerTable, ct).ConfigureAwait(false);
                                    if (stats.SampledEntities == 0 && pruneOptions.PruneEmptyTables)
                                    {
                                        results.Add(new TablePruningOperations()
                                        {
                                            TableName = tableName,
                                            WasDeleted = false,
                                            WouldDelete = true,
                                            DeleteReason = "Empty table"

                                        });
                                    }
                                    else if (stats.LastWritten < DateTime.UtcNow.AddMonths(-12) && pruneOptions.PruneBasedOnLastWrite)
                                    {
                                        var delta = DateTime.UtcNow - stats.LastWritten;
                                        results.Add(new TablePruningOperations()
                                        {
                                            TableName = tableName,
                                            WasDeleted = false,
                                            WouldDelete = true,
                                            DeleteReason = $"Last written {delta.Days} days ago"
                                        });

                                        _logger.Trace($"{this.Tag()} - {thisIdx} - End {tableName} found {stats.SampledEntities} in {sw.Elapsed.TotalSeconds} seconds");
                                    }
                                    else
                                    {
                                        _logger.Trace($"{this.Tag()} - {thisIdx} - End {tableName} found {stats.SampledEntities} in {sw.Elapsed.TotalSeconds} seconds");
                                        results.Add(new TablePruningOperations()
                                        {
                                            TableName = tableName,
                                            WasDeleted = false,
                                            WouldDelete = false,
                                            DeleteReason = "Does not meet pruning criteria"
                                        });
                                    }
                                }
                            }
                            else
                            {
                                _logger.Trace($"{this.Tag()} - {thisIdx} - Skipping system table {tableName}");
                                results.Add(new TablePruningOperations()
                                {
                                    TableName = tableName,
                                    WasDeleted = false,
                                    WouldDelete = false,
                                    DeleteReason = "System table or does not meet pruning criteria"
                                });

                            }
              
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
                    .OrderByDescending(r => r.TableName)
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

                var timeStamp = DateTime.MinValue;

                // For sampling, we read full entities. If you already know which columns can be huge,
                // you can set "select" to only keys + those columns to reduce payload.
                await foreach (var entity in tableClient.QueryAsync<TableEntity>(maxPerPage: 200, cancellationToken: ct))
                {
                    var est = EstimateEntityBytes(entity);

                    stats.SampledBytes += est;
                    stats.SampledEntities++;

                    if(timeStamp == DateTime.MinValue)
                        stats.LastWritten = entity.Timestamp?.UtcDateTime ?? DateTime.MinValue;

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

            public static DateTime FromRowKeyDatePart(string rowKeyDatePart)
            {
                if (string.IsNullOrWhiteSpace(rowKeyDatePart))
                    throw new ArgumentException("rowKeyDatePart is required.", nameof(rowKeyDatePart));

                if (!long.TryParse(rowKeyDatePart, NumberStyles.None, CultureInfo.InvariantCulture, out var reversedTicks))
                    throw new FormatException("rowKeyDatePart is not a valid 19-digit tick value.");

                var ticks = DateTime.MaxValue.Ticks - reversedTicks;

                // Guard against invalid values
                if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                    throw new ArgumentOutOfRangeException(nameof(rowKeyDatePart), "RowKey date part decodes to an invalid DateTime tick range.");

                return new DateTime(ticks, DateTimeKind.Utc); // or Unspecified, depending on how you created the original DateTime
            }

            public static DateTime FromRowKey(string rowKey)
            {
                if (string.IsNullOrWhiteSpace(rowKey))
                    throw new ArgumentException("rowKey is required.", nameof(rowKey));

             
                // take the first 19 digits
                var datePart = rowKey.Length >= 19 ? rowKey.Substring(0, 19) : rowKey;

                return FromRowKeyDatePart(datePart);
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