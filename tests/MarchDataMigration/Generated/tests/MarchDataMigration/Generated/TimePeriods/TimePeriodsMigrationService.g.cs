using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.TimePeriods
{
    // generated: orchestration, preserved mapping seam
    public sealed class TimePeriodsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await TimePeriodsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => TimePeriodsMapper.Map(source)).ToList();

                var table = TimePeriodsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    TimePeriodsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.TimePeriods;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    TimePeriodsTableDefinition.TableName,
                    table,
                    TimePeriodsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, TimePeriodsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = TimePeriodsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(TimePeriodsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
