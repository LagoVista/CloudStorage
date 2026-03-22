// Aptix File Bundle
// root: .

// FILE: tests/MarchDataMigration/Infrastructure/TableMigrationResult.cs
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using MarchDataMigration.Generated.AppUser;
using MarchDataMigration.Infrastructure;
using MarchDataMigration.Mappings;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Infrastructure
{
    public sealed class TableMigrationResult
    {
        public string TableName { get; init; }
        public int SourceCount { get; init; }
        public int InsertedCount { get; init; }
        public int TargetCountAfterInsert { get; init; }
        public TimeSpan Duration { get; init; }
        public bool Success { get; init; }
        public string ErrorMessage { get; init; }

        public static TableMigrationResult Failure(string tableName, TimeSpan duration, Exception ex)
        {
            return new TableMigrationResult
            {
                TableName = tableName,
                Duration = duration,
                Success = false,
                ErrorMessage = ex.ToString()
            };
        }
    }
}

namespace MarchDataMigration.Infrastructure
{
    public static class MigrationConnectionFactory
    {
        public static string Build(ConnectionSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var csb = new SqlConnectionStringBuilder
            {
                DataSource = settings.Uri,
                InitialCatalog = settings.ResourceName,
                UserID = settings.UserName,
                Password = settings.Password,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                ConnectTimeout = settings.TimeoutInSeconds > 0 ? settings.TimeoutInSeconds : 30
            };

            return csb.ConnectionString;
        }

        public static string Create(string databaseName)
        {
            var settings = TestConnections.DevSQLServer;
            settings.ResourceName = databaseName;
            return Build(settings);
        }
    }
}


namespace MarchDataMigration.Infrastructure
{
    public static class SqlBulkInsertService
    {
        public static async Task<int> BulkInsertAsync(
            string connectionString,
            string destinationTableName,
            DataTable dataTable,
            Action<SqlBulkCopy> configureMappings,
            CancellationToken ct,
            int batchSize = 1000,
            int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            ArgumentNullException.ThrowIfNull(destinationTableName);
            ArgumentNullException.ThrowIfNull(dataTable);
            ArgumentNullException.ThrowIfNull(configureMappings);

            if (dataTable.Rows.Count == 0)
                return 0;

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, null)
            {
                DestinationTableName = destinationTableName,
                BatchSize = batchSize,
                BulkCopyTimeout = timeoutSeconds
            };

            configureMappings(bulk);
            await bulk.WriteToServerAsync(dataTable, ct);

            return dataTable.Rows.Count;
        }

        public static async Task ExecuteAsync(string connectionString, string sql, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            ArgumentNullException.ThrowIfNull(sql);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await command.ExecuteNonQueryAsync(ct);
        }

        public static async Task<int> CountAsync(string connectionString, string tableName, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            ArgumentNullException.ThrowIfNull(tableName);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand($"SELECT COUNT(*) FROM {tableName};", connection)
            {
                CommandTimeout = timeoutSeconds
            };

            var result = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result);
        }
    }
}

namespace MarchDataMigration.Infrastructure
{
    public abstract class MigrationTestBase
    {
        protected const string SourceDatabaseName = "nuviot-dev";
        protected const string TargetDatabaseName = "nuviot-dev-2026-03-22";

        protected string SourceConnectionString => MigrationConnectionFactory.Create(SourceDatabaseName);
        protected string TargetConnectionString => MigrationConnectionFactory.Create(TargetDatabaseName);
    }
}

namespace MarchDataMigration.Generated.AppUser
{
    // generated: source-side 1:1 shape
    public sealed class SourceAppUserRow
    {
        public string AppUserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
    }
}

namespace MarchDataMigration.Generated.AppUser
{
    // generated: target-side 1:1 shape
    public partial class TargetAppUserRow
    {
        public string AppUserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
    }
}


namespace MarchDataMigration.Generated.AppUser
{
    // generated: target table contract
    public static class AppUserTableDefinition
    {
        public const string TableName = "dbo.AppUser";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("AppUserId", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("FullName", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetAppUserRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.AppUserId,
                row.Email,
                row.FullName,
                row.CreationDate,
                row.LastUpdatedDate);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("AppUserId", "AppUserId");
            bulk.ColumnMappings.Add("Email", "Email");
            bulk.ColumnMappings.Add("FullName", "FullName");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
        }
    }
}


namespace MarchDataMigration.Generated.AppUser
{
    // generated: full source query and reader
    public static class AppUserSourceReader
    {
        public const string SourceQuery = @"
SELECT
    AppUserId,
    Email,
    FullName,
    CreationDate,
    LastUpdatedDate
FROM dbo.AppUser
ORDER BY AppUserId;";

        public static async Task<List<SourceAppUserRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceAppUserRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceAppUserRow
                {
                    AppUserId = reader["AppUserId"].ToString(),
                    Email = reader["Email"].ToString(),
                    FullName = reader["FullName"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdatedDate = reader.GetDateTime(reader.GetOrdinal("LastUpdatedDate"))
                });
            }

            return rows;
        }
    }
}


namespace MarchDataMigration.Generated.AppUser
{
    // generated: orchestration, preserved mapping seam
    public sealed class AppUserMigrationService
    {
        public async Task<TableMigrationResult> MigrateAsync(
            string sourceConnectionString,
            string targetConnectionString,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var sourceRows = await AppUserSourceReader.LoadAsync(sourceConnectionString, ct);
                var targetRows = sourceRows.Select(AppUserMapper.Map).ToList();

                var table = AppUserTableDefinition.CreateDataTable();
                foreach (var row in targetRows)
                {
                    AppUserTableDefinition.AddRow(table, row);
                }

                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, "DELETE FROM dbo.AppUser;", ct);

                var inserted = await SqlBulkInsertService.BulkInsertAsync(
                    targetConnectionString,
                    AppUserTableDefinition.TableName,
                    table,
                    AppUserTableDefinition.ConfigureMappings,
                    ct);

                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, AppUserTableDefinition.TableName, ct);

                return new TableMigrationResult
                {
                    TableName = AppUserTableDefinition.TableName,
                    SourceCount = sourceRows.Count,
                    InsertedCount = inserted,
                    TargetCountAfterInsert = targetCount,
                    Duration = sw.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return TableMigrationResult.Failure(AppUserTableDefinition.TableName, sw.Elapsed, ex);
            }
        }
    }
}


namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class AppUserMapper
    {
        public static TargetAppUserRow Map(SourceAppUserRow source)
        {
            return new TargetAppUserRow
            {
                AppUserId = source.AppUserId,
                Email = source.Email,
                FullName = source.FullName,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdatedDate
            };
        }
    }
}


namespace MarchDataMigration.Tests
{
    [TestFixture]
    public class AppUserMigrationTests : MigrationTestBase
    {
        [Test]
        public async Task Migrate_AppUser()
        {
            var service = new AppUserMigrationService();

            var result = await service.MigrateAsync(SourceConnectionString, TargetConnectionString, CancellationToken.None);

            TestContext.WriteLine(
                $"table={result.TableName} success={result.Success} source={result.SourceCount} inserted={result.InsertedCount} target={result.TargetCountAfterInsert} duration={result.Duration}");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceCount, Is.EqualTo(result.InsertedCount), "Source/insert count mismatch.");
            Assert.That(result.TargetCountAfterInsert, Is.EqualTo(result.InsertedCount), "Target/insert count mismatch.");
        }
    }
}

// FILE: tests/MarchDataMigration/Generator/GeneratorContract.cs
namespace MarchDataMigration.Generator
{
    // handwritten once: this is the contract I'd generate against for every table
    public sealed class GeneratorContract
    {
        public string TableName { get; init; }
        public string SourceTableName { get; init; }
        public string TargetTableName { get; init; }
        public string[] OrderedColumns { get; init; }
        public string OrderByColumn { get; init; }
    }
}