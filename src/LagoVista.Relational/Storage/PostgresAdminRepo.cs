using LagoVista.CloudStorage.Storage.ConnectionSettings;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Relational.Storage
{
    public interface IPostgresAdminRepo
    {
        Task<IReadOnlyList<PostgresDatabaseInfo>> GetDatabasesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetSchemasAsync(string database, CancellationToken ct = default);
        Task<IReadOnlyList<PostgresTableInfo>> GetTablesAsync(string database, string schema, CancellationToken ct = default);
        Task<PostgresQueryResult> QueryAsync(PostgresQueryRequest request, CancellationToken ct = default);
    }

    /// <summary>
    /// Bounded, read-only PostgreSQL explorer for the cluster's existing app account.
    /// This never accepts host, port, user or password from the caller.
    /// </summary>
    public sealed class PostgresAdminRepo : IPostgresAdminRepo
    {
        private const int MaximumRows = 500;
        private const int CommandTimeoutSeconds = 15;
        private readonly IPostgresConnectionSettings _settings;

        public PostgresAdminRepo(IPostgresConnectionSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public static int BoundPageSize(int requested) => Math.Max(1, Math.Min(requested, MaximumRows));

        public static string ValidateReadOnlySql(string sql)
        {
            if (String.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL is required.", nameof(sql));

            var normalized = sql.Trim();
            if (normalized.Length > 16384)
                throw new ArgumentException("SQL must be at most 16,384 characters.", nameof(sql));

            if (normalized.EndsWith(";", StringComparison.Ordinal))
                normalized = normalized.Substring(0, normalized.Length - 1).TrimEnd();

            // Single statement only. Semicolons inside SQL literals are deliberately rejected
            // for this operational query surface rather than trying to parse SQL client-side.
            if (normalized.Contains(';') ||
                !Regex.IsMatch(normalized, @"^(SELECT|WITH|VALUES|TABLE)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                throw new ArgumentException("Only one read-only SELECT/WITH/VALUES/TABLE statement is allowed.", nameof(sql));

            // Transaction READ ONLY is the authoritative protection, including data-modifying
            // CTEs and SELECT INTO; the prefix rule is only an additional guard.
            return normalized;
        }

        private async Task<NpgsqlConnection> OpenAsync(string database, CancellationToken ct)
        {
            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = _settings.HostName,
                Port = _settings.Port,
                Database = database,
                Username = _settings.UserName,
                Password = _settings.Password,
                Timeout = 5,
                CommandTimeout = CommandTimeoutSeconds
            }.ConnectionString;
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync(ct).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task<IReadOnlyList<PostgresDatabaseInfo>> GetDatabasesAsync(CancellationToken ct = default)
        {
            await using var connection = await OpenAsync("postgres", ct).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(
                "SELECT datname FROM pg_catalog.pg_database WHERE datallowconn AND NOT datistemplate " +
                "AND has_database_privilege(datname, 'CONNECT') ORDER BY datname", connection);
            var result = new List<PostgresDatabaseInfo>();
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                result.Add(new PostgresDatabaseInfo { Name = reader.GetString(0) });
            return result;
        }

        private async Task EnsureDatabaseAllowedAsync(string database, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Database is required.", nameof(database));
            var databases = await GetDatabasesAsync(ct).ConfigureAwait(false);
            var found = false;
            foreach (var item in databases)
                if (String.Equals(item.Name, database, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            if (!found)
                throw new ArgumentException("Database is not available to the configured PostgreSQL account.", nameof(database));
        }

        public async Task<IReadOnlyList<string>> GetSchemasAsync(string database, CancellationToken ct = default)
        {
            await EnsureDatabaseAllowedAsync(database, ct).ConfigureAwait(false);
            await using var connection = await OpenAsync(database, ct).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(
                "SELECT nspname FROM pg_catalog.pg_namespace " +
                "WHERE nspname NOT LIKE 'pg_%' AND nspname <> 'information_schema' " +
                "ORDER BY nspname", connection);
            var schemas = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                schemas.Add(reader.GetString(0));
            return schemas;
        }

        public async Task<IReadOnlyList<PostgresTableInfo>> GetTablesAsync(string database, string schema, CancellationToken ct = default)
        {
            await EnsureDatabaseAllowedAsync(database, ct).ConfigureAwait(false);
            if (String.IsNullOrWhiteSpace(schema))
                throw new ArgumentException("Schema is required.", nameof(schema));

            await using var connection = await OpenAsync(database, ct).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(
                "SELECT n.nspname, c.relname, " +
                "CASE c.relkind " +
                "WHEN 'r' THEN 'BASE TABLE' WHEN 'p' THEN 'PARTITIONED TABLE' " +
                "WHEN 'v' THEN 'VIEW' WHEN 'm' THEN 'MATERIALIZED VIEW' " +
                "WHEN 'f' THEN 'FOREIGN TABLE' END " +
                "FROM pg_catalog.pg_class c " +
                "JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace " +
                "WHERE n.nspname = @schema AND c.relkind IN ('r', 'p', 'v', 'm', 'f') " +
                "ORDER BY c.relname", connection);
            command.Parameters.AddWithValue("schema", schema);
            var result = new List<PostgresTableInfo>();
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                result.Add(new PostgresTableInfo
                {
                    Schema = reader.GetString(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2)
                });
            return result;
        }

        public async Task<PostgresQueryResult> QueryAsync(PostgresQueryRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var sql = ValidateReadOnlySql(request.Sql);
            await EnsureDatabaseAllowedAsync(request.Database, ct).ConfigureAwait(false);
            var size = BoundPageSize(request.PageSize);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(CommandTimeoutSeconds));
            var token = timeout.Token;
            await using var connection = await OpenAsync(request.Database, token).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token).ConfigureAwait(false);
            await using (var protect = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
                await protect.ExecuteNonQueryAsync(token).ConfigureAwait(false);

            var result = new PostgresQueryResult();
            await using (var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = CommandTimeoutSeconds })
            await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token).ConfigureAwait(false))
            {
                for (var index = 0; index < reader.FieldCount; index++)
                    result.Columns.Add(reader.GetName(index));

                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (result.Rows.Count == size)
                    {
                        result.Truncated = true;
                        break;
                    }

                    var row = new Dictionary<string, object>();
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        var value = await reader.IsDBNullAsync(index, token).ConfigureAwait(false)
                            ? null : reader.GetValue(index);
                        row[result.Columns[index]] = ToJsonValue(value);
                    }
                    result.Rows.Add(JsonSerializer.Serialize(row));
                }
            }
            await transaction.RollbackAsync(token).ConfigureAwait(false);
            return result;
        }

        private static object ToJsonValue(object value)
        {
            if (value == null || value is string || value is bool || value is int || value is long ||
                value is short || value is byte || value is decimal || value is float || value is double ||
                value is Guid || value is DateTime || value is DateTimeOffset)
                return value;
            if (value is byte[] bytes)
                return Convert.ToBase64String(bytes);
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
