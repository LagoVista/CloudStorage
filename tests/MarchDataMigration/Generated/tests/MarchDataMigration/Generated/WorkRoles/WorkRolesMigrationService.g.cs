using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.WorkRoles
{
    // generated: orchestration, preserved mapping seam
    public sealed class WorkRolesMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await WorkRolesSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => WorkRolesMapper.Map(source)).ToList();

                var table = WorkRolesTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    WorkRolesTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.WorkRoles;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    WorkRolesTableDefinition.TableName,
                    table,
                    WorkRolesTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, WorkRolesTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = WorkRolesTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(WorkRolesTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
