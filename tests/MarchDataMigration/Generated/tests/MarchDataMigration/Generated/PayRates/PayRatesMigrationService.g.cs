using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.PayRates
{
    // generated: orchestration, preserved mapping seam
    public sealed class PayRatesMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await PayRatesSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => PayRatesMapper.Map(source)).ToList();

                var table = PayRatesTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    PayRatesTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.PayRates;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    PayRatesTableDefinition.TableName,
                    table,
                    PayRatesTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, PayRatesTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = PayRatesTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(PayRatesTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
