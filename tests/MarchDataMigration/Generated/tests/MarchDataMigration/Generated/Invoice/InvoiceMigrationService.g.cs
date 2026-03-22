using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Invoice
{
    // generated: orchestration, preserved mapping seam
    public sealed class InvoiceMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await InvoiceSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => InvoiceMapper.Map(source)).ToList();

                var table = InvoiceTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    InvoiceTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Invoice;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    InvoiceTableDefinition.TableName,
                    table,
                    InvoiceTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, InvoiceTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = InvoiceTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(InvoiceTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
