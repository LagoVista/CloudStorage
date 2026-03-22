using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Customers
{
    // generated: orchestration, preserved mapping seam
    public sealed class CustomersMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await CustomersSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => CustomersMapper.Map(source)).ToList();

                var table = CustomersTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    CustomersTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Customers;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    CustomersTableDefinition.TableName,
                    table,
                    CustomersTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, CustomersTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = CustomersTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(CustomersTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
