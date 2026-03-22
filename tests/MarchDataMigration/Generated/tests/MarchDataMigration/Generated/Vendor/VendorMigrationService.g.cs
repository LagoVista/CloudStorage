using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Vendor
{
    // generated: orchestration, preserved mapping seam
    public sealed class VendorMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await VendorSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => VendorMapper.Map(source)).ToList();

                var table = VendorTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    VendorTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Vendor;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    VendorTableDefinition.TableName,
                    table,
                    VendorTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, VendorTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = VendorTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(VendorTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
