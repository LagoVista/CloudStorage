using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductIncluded
{
    // generated: orchestration, preserved mapping seam
    public sealed class ProductIncludedMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await ProductIncludedSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => ProductIncludedMapper.Map(source)).ToList();

                var table = ProductIncludedTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    ProductIncludedTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.ProductIncluded;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    ProductIncludedTableDefinition.TableName,
                    table,
                    ProductIncludedTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, ProductIncludedTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = ProductIncludedTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(ProductIncludedTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
