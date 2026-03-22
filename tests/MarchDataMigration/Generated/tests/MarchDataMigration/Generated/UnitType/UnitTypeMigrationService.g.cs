using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.UnitType
{
    // generated: orchestration, preserved mapping seam
    public sealed class UnitTypeMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await UnitTypeSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => UnitTypeMapper.Map(source)).ToList();

                var table = UnitTypeTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    UnitTypeTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.UnitType;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    UnitTypeTableDefinition.TableName,
                    table,
                    UnitTypeTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, UnitTypeTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = UnitTypeTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(UnitTypeTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
