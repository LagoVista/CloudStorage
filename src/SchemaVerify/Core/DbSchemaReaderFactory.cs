using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace SchemaVerify.Core;

public sealed class DbSchemaReaderFactory : IDbSchemaReaderFactory
{
    public IDbSchemaReader Create(string provider)
    {
        provider = (provider ?? string.Empty).Trim().ToLowerInvariant();
        return provider switch
        {
            "sqlserver" => new SqlServerSchemaReader(),
            "postgres" or "postgresql" => new PostgresSchemaReader(),
            _ => throw new InvalidOperationException($"Unsupported provider '{provider}'. Use 'sqlserver' or 'postgres'.")
        };
    }

    public DbConnection CreateConnection(string provider, string connectionString)
    {
        provider = (provider ?? string.Empty).Trim().ToLowerInvariant();
        return provider switch
        {
            "sqlserver" => new SqlConnection(connectionString),
            "postgres" or "postgresql" => new NpgsqlConnection(connectionString),
            _ => throw new InvalidOperationException($"Unsupported provider '{provider}'.")
        };
    }
}
