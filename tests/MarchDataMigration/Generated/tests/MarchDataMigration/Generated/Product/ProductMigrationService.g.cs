using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Product
{
    // generated: orchestration, preserved mapping seam
    public sealed class ProductMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await ProductSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => ProductMapper.Map(source)).ToList();

                var table = ProductTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    ProductTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Product;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    ProductTableDefinition.TableName,
                    table,
                    ProductTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, ProductTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = ProductTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(ProductTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
