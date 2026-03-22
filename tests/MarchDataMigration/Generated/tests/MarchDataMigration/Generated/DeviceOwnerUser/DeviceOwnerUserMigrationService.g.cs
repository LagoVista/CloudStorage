using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.DeviceOwnerUser
{
    // generated: orchestration, preserved mapping seam
    public sealed class DeviceOwnerUserMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await DeviceOwnerUserSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => DeviceOwnerUserMapper.Map(source)).ToList();

                var table = DeviceOwnerUserTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    DeviceOwnerUserTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.DeviceOwnerUser;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    DeviceOwnerUserTableDefinition.TableName,
                    table,
                    DeviceOwnerUserTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, DeviceOwnerUserTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = DeviceOwnerUserTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(DeviceOwnerUserTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
