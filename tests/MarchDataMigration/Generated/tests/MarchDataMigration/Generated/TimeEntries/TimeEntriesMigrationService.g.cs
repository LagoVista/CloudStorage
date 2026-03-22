using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.TimeEntries
{
    // generated: orchestration, preserved mapping seam
    public sealed class TimeEntriesMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await TimeEntriesSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => TimeEntriesMapper.Map(source)).ToList();

                var table = TimeEntriesTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    TimeEntriesTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.TimeEntries;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    TimeEntriesTableDefinition.TableName,
                    table,
                    TimeEntriesTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, TimeEntriesTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = TimeEntriesTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(TimeEntriesTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
