using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.BillingEvents
{
    // generated: orchestration, preserved mapping seam
    public sealed class BillingEventsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await BillingEventsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => BillingEventsMapper.Map(source)).ToList();

                var table = BillingEventsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    BillingEventsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.BillingEvents;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    BillingEventsTableDefinition.TableName,
                    table,
                    BillingEventsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, BillingEventsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = BillingEventsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(BillingEventsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
