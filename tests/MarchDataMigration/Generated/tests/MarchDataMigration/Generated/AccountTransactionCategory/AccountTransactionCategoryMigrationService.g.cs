using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.AccountTransactionCategory
{
    // generated: orchestration, preserved mapping seam
    public sealed class AccountTransactionCategoryMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await AccountTransactionCategorySourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => AccountTransactionCategoryMapper.Map(source)).ToList();

                var table = AccountTransactionCategoryTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    AccountTransactionCategoryTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.AccountTransactionCategory;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    AccountTransactionCategoryTableDefinition.TableName,
                    table,
                    AccountTransactionCategoryTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, AccountTransactionCategoryTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = AccountTransactionCategoryTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(AccountTransactionCategoryTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
