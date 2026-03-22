using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.InvoiceLineItems
{
    // generated: orchestration, preserved mapping seam
    public sealed class InvoiceLineItemsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await InvoiceLineItemsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => InvoiceLineItemsMapper.Map(source)).ToList();

                var table = InvoiceLineItemsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    InvoiceLineItemsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.InvoiceLineItems;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    InvoiceLineItemsTableDefinition.TableName,
                    table,
                    InvoiceLineItemsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, InvoiceLineItemsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = InvoiceLineItemsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(InvoiceLineItemsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
