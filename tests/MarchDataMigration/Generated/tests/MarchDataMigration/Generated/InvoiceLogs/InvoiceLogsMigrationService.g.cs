using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.InvoiceLogs
{
    // generated: orchestration, preserved mapping seam
    public sealed class InvoiceLogsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await InvoiceLogsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => InvoiceLogsMapper.Map(source)).ToList();

                var table = InvoiceLogsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    InvoiceLogsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.InvoiceLogs;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    InvoiceLogsTableDefinition.TableName,
                    table,
                    InvoiceLogsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, InvoiceLogsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = InvoiceLogsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(InvoiceLogsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
