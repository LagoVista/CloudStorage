using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.DeviceOwnerUserDevices
{
    // generated: orchestration, preserved mapping seam
    public sealed class DeviceOwnerUserDevicesMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await DeviceOwnerUserDevicesSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => DeviceOwnerUserDevicesMapper.Map(source)).ToList();

                var table = DeviceOwnerUserDevicesTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    DeviceOwnerUserDevicesTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.DeviceOwnerUserDevices;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    DeviceOwnerUserDevicesTableDefinition.TableName,
                    table,
                    DeviceOwnerUserDevicesTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, DeviceOwnerUserDevicesTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = DeviceOwnerUserDevicesTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(DeviceOwnerUserDevicesTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
