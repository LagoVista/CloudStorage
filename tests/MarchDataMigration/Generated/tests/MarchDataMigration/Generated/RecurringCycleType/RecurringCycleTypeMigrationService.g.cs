using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.RecurringCycleType
{
    // generated: orchestration, preserved mapping seam
    public sealed class RecurringCycleTypeMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await RecurringCycleTypeSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => RecurringCycleTypeMapper.Map(source)).ToList();

                var table = RecurringCycleTypeTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    RecurringCycleTypeTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.RecurringCycleType;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    RecurringCycleTypeTableDefinition.TableName,
                    table,
                    RecurringCycleTypeTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, RecurringCycleTypeTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = RecurringCycleTypeTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(RecurringCycleTypeTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
