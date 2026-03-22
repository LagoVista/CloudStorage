using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductPage_Product
{
    // generated: orchestration, preserved mapping seam
    public sealed class ProductPage_ProductMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await ProductPage_ProductSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => ProductPage_ProductMapper.Map(source)).ToList();

                var table = ProductPage_ProductTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    ProductPage_ProductTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.ProductPage_Product;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    ProductPage_ProductTableDefinition.TableName,
                    table,
                    ProductPage_ProductTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, ProductPage_ProductTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = ProductPage_ProductTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(ProductPage_ProductTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
