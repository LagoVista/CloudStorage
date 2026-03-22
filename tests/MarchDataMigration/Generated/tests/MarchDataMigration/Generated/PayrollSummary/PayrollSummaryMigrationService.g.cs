using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.PayrollSummary
{
    // generated: orchestration, preserved mapping seam
    public sealed class PayrollSummaryMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await PayrollSummarySourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => PayrollSummaryMapper.Map(source)).ToList();

                var table = PayrollSummaryTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    PayrollSummaryTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.PayrollRun;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    PayrollSummaryTableDefinition.TableName,
                    table,
                    PayrollSummaryTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, PayrollSummaryTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = PayrollSummaryTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(PayrollSummaryTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
