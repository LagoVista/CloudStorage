using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Payments
{
    // generated: orchestration, preserved mapping seam
    public sealed class PaymentsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await PaymentsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => PaymentsMapper.Map(source)).ToList();

                var table = PaymentsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    PaymentsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Payments;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    PaymentsTableDefinition.TableName,
                    table,
                    PaymentsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, PaymentsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = PaymentsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(PaymentsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
