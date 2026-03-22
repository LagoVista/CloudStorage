//using Microsoft.Data.SqlClient;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace MarchDataMigration
//{
//    public abstract class SqlBulkMigrationStep : ITableMigrationStep
//    {
//        private readonly string _sourceConnectionString;
//        private readonly string _targetConnectionString;

//        protected SqlBulkMigrationStep(string sourceConnectionString, string targetConnectionString)
//        {
//            _sourceConnectionString = sourceConnectionString ?? throw new ArgumentNullException(nameof(sourceConnectionString));
//            _targetConnectionString = targetConnectionString ?? throw new ArgumentNullException(nameof(targetConnectionString));
//        }

//        public abstract string Name { get; }
//        protected abstract string SourceQuery { get; }
//        protected abstract string DestinationTableName { get; }

//        protected virtual int CommandTimeoutSeconds => 120;
//        protected virtual int BulkCopyTimeoutSeconds => 120;
//        protected virtual int BatchSize => 1000;
//        protected virtual bool ClearTargetBeforeInsert => false;
//        protected virtual string ClearTargetSql => $"DELETE FROM {DestinationTableName};";

//        protected abstract DataTable CreateDataTable();
//        protected abstract void AddRow(DataTable table, SqlDataReader reader);
//        protected abstract void ConfigureColumnMappings(SqlBulkCopy bulkCopy);

//        public async Task<TableMigrationResult> ExecuteAsync(CancellationToken ct)
//        {
//            var sw = Stopwatch.StartNew();

//            try
//            {
//                var table = CreateDataTable();

//                await using (var source = new SqlConnection(_sourceConnectionString))
//                {
//                    await source.OpenAsync(ct);

//                    await using var cmd = new SqlCommand(SourceQuery, source)
//                    {
//                        CommandTimeout = CommandTimeoutSeconds
//                    };

//                    await using var reader = await cmd.ExecuteReaderAsync(ct);

//                    while (await reader.ReadAsync(ct))
//                    {
//                        AddRow(table, reader);
//                    }
//                }

//                await using (var target = new SqlConnection(_targetConnectionString))
//                {
//                    await target.OpenAsync(ct);

//                    if (ClearTargetBeforeInsert)
//                    {
//                        await using var clear = new SqlCommand(ClearTargetSql, target)
//                        {
//                            CommandTimeout = CommandTimeoutSeconds
//                        };

//                        await clear.ExecuteNonQueryAsync(ct);
//                    }

//                    if (table.Rows.Count > 0)
//                    {
//                        using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.CheckConstraints, null)
//                        {
//                            DestinationTableName = DestinationTableName,
//                            BatchSize = BatchSize,
//                            BulkCopyTimeout = BulkCopyTimeoutSeconds
//                        };

//                        ConfigureColumnMappings(bulk);
//                        await bulk.WriteToServerAsync(table, ct);
//                    }

//                    var targetCountAfterInsert = await CountTargetRowsAsync(target, ct);

//                    return new TableMigrationResult
//                    {
//                        TableName = Name,
//                        SourceCount = table.Rows.Count,
//                        InsertedCount = table.Rows.Count,
//                        TargetCountAfterInsert = targetCountAfterInsert,
//                        Duration = sw.Elapsed,
//                        Success = true
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                return new TableMigrationResult
//                {
//                    TableName = Name,
//                    Duration = sw.Elapsed,
//                    Success = false,
//                    ErrorMessage = ex.ToString()
//                };
//            }
//        }

//        private async Task<int> CountTargetRowsAsync(SqlConnection target, CancellationToken ct)
//        {
//            await using var cmd = new SqlCommand($"SELECT COUNT(*) FROM {DestinationTableName};", target)
//            {
//                CommandTimeout = CommandTimeoutSeconds
//            };

//            var result = await cmd.ExecuteScalarAsync(ct);
//            return Convert.ToInt32(result);
//        }

//        protected static object DbValue(string value)
//        {
//            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
//        }

//        protected static object DbValue(DateTime? value)
//        {
//            return value.HasValue ? value.Value : DBNull.Value;
//        }

//        protected static object DbValue(Guid? value)
//        {
//            return value.HasValue ? value.Value : DBNull.Value;
//        }

//        protected static object DbValue(decimal? value)
//        {
//            return value.HasValue ? value.Value : DBNull.Value;
//        }

//        protected static object DbValue(int? value)
//        {
//            return value.HasValue ? value.Value : DBNull.Value;
//        }

//        protected static object DbValue(bool? value)
//        {
//            return value.HasValue ? value.Value : DBNull.Value;
//        }
//    }
//}
