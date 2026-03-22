using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Org
{
    // generated: orchestration, preserved mapping seam
    public sealed class OrgMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await OrgSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => OrgMapper.Map(source)).ToList();

                var table = OrgTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    OrgTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Org;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    OrgTableDefinition.TableName,
                    table,
                    OrgTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, OrgTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = OrgTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(OrgTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
