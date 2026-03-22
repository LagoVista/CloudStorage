using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Account
{
    // generated: orchestration, preserved mapping seam
    public sealed class AccountMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await AccountSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => AccountMapper.Map(source)).ToList();

                var table = AccountTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    AccountTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Account;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    AccountTableDefinition.TableName,
                    table,
                    AccountTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, AccountTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = AccountTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(AccountTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
