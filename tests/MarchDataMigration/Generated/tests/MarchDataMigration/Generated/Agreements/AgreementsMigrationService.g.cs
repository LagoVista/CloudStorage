using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Agreements
{
    // generated: orchestration, preserved mapping seam
    public sealed class AgreementsMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await AgreementsSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(source => AgreementsMapper.Map(source)).ToList();

                var table = AgreementsTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    AgreementsTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.Agreements;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    AgreementsTableDefinition.TableName,
                    table,
                    AgreementsTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, AgreementsTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = AgreementsTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(AgreementsTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}
