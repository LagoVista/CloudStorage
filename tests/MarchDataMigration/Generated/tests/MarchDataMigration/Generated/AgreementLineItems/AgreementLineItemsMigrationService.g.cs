using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.AgreementLineItems
{
    // generated: orchestration, preserved mapping seam
    public sealed class AgreementLineItemsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await AgreementLineItemsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => AgreementLineItemsMapper.Map(source)).ToList();

                var table = AgreementLineItemsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    AgreementLineItemsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.AgreementLineItems;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    AgreementLineItemsTableDefinition.TableName,
                    table,
                    AgreementLineItemsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, AgreementLineItemsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = AgreementLineItemsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(AgreementLineItemsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
