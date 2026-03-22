using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Infrastructure
{
    public static class SqlBulkInsertService
    {
        public static async Task<int> BulkInsertAsync(string connectionString, string destinationTableName, DataTable dataTable, Action<SqlBulkCopy> configureMappings, CancellationToken ct, int batchSize = 1000, int timeoutSeconds = 120)
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

            Console.WriteLine(sql);

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
