using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.AccountTransaction
{
    // generated: orchestration, preserved mapping seam
    public sealed class AccountTransactionMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await AccountTransactionSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => AccountTransactionMapper.Map(source)).ToList();

                var table = AccountTransactionTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    AccountTransactionTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.AccountTransaction;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    AccountTransactionTableDefinition.TableName,
                    table,
                    AccountTransactionTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, AccountTransactionTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = AccountTransactionTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(AccountTransactionTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
