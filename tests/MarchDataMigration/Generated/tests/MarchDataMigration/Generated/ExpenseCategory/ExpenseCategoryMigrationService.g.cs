using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ExpenseCategory
{
    // generated: orchestration, preserved mapping seam
    public sealed class ExpenseCategoryMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await ExpenseCategorySourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => ExpenseCategoryMapper.Map(source)).ToList();

                var table = ExpenseCategoryTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    ExpenseCategoryTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.ExpenseCategory;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    ExpenseCategoryTableDefinition.TableName,
                    table,
                    ExpenseCategoryTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, ExpenseCategoryTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = ExpenseCategoryTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(ExpenseCategoryTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
