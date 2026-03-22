using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.BudgetItems
{
    // generated: orchestration, preserved mapping seam
    public sealed class BudgetItemsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await BudgetItemsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => BudgetItemsMapper.Map(source)).ToList();

                var table = BudgetItemsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    BudgetItemsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.BudgetItems;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    BudgetItemsTableDefinition.TableName,
                    table,
                    BudgetItemsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, BudgetItemsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = BudgetItemsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(BudgetItemsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
