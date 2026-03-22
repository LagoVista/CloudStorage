using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductCategory
{
    // generated: orchestration, preserved mapping seam
    public sealed class ProductCategoryMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await ProductCategorySourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => ProductCategoryMapper.Map(source)).ToList();

                var table = ProductCategoryTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    ProductCategoryTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.ProductCategory;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    ProductCategoryTableDefinition.TableName,
                    table,
                    ProductCategoryTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, ProductCategoryTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = ProductCategoryTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(ProductCategoryTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
