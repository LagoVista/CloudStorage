using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Expenses
{
    // generated: orchestration, preserved mapping seam
    public sealed class ExpensesMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await ExpensesSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => ExpensesMapper.Map(source)).ToList();

                var table = ExpensesTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    ExpensesTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Expenses;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    ExpensesTableDefinition.TableName,
                    table,
                    ExpensesTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, ExpensesTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = ExpensesTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(ExpensesTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
