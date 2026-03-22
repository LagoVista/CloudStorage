using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductPage
{
    // generated: orchestration, preserved mapping seam
    public sealed class ProductPageMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await ProductPageSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => ProductPageMapper.Map(source)).ToList();

                var table = ProductPageTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    ProductPageTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.ProductPage;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    ProductPageTableDefinition.TableName,
                    table,
                    ProductPageTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, ProductPageTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = ProductPageTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(ProductPageTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
