using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Subscription
{
    // generated: orchestration, preserved mapping seam
    public sealed class SubscriptionMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await SubscriptionSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => SubscriptionMapper.Map(source)).ToList();

                var table = SubscriptionTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    SubscriptionTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Subscription;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    SubscriptionTableDefinition.TableName,
                    table,
                    SubscriptionTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, SubscriptionTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = SubscriptionTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(SubscriptionTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
