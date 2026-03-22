using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.SerialNumbers
{
    // generated: orchestration, preserved mapping seam
    public sealed class SerialNumbersMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await SerialNumbersSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => SerialNumbersMapper.Map(source)).ToList();

                var table = SerialNumbersTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    SerialNumbersTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.SerialNumbers;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    SerialNumbersTableDefinition.TableName,
                    table,
                    SerialNumbersTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, SerialNumbersTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = SerialNumbersTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(SerialNumbersTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
