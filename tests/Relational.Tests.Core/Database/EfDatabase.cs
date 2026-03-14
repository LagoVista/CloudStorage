using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Relational.Tests.Core.Database;

public enum EfTestProvider
{
    SqliteInMemory,
    SqlServer,
    Postgres
}

public sealed class EfTestDatabase : IAsyncDisposable
{
    public EfTestProvider Provider { get; }
    public string DatabaseName { get; }
    public string ConnectionString { get; }
    public DbConnection SharedConnection { get; }

    private EfTestDatabase(EfTestProvider provider, string dbName, string connStr, DbConnection sharedConn)
    {
        Provider = provider;
        DatabaseName = dbName;
        ConnectionString = connStr;
        SharedConnection = sharedConn;
    }

    public static async Task<EfTestDatabase> CreateAsync(EfTestProvider provider, string baseConnectionString = null, string dbName = null)
    {
        dbName ??= $"test_{Guid.NewGuid():N}";

        switch (provider)
        {
            case EfTestProvider.SqliteInMemory:
                {
                    // Important: keep this connection OPEN for the DB lifetime
                    var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
                    await conn.OpenAsync().ConfigureAwait(false);
                    return new EfTestDatabase(provider, dbName, conn.ConnectionString, conn);
                }

            case EfTestProvider.SqlServer:
                {
                    if (string.IsNullOrWhiteSpace(baseConnectionString))
                        throw new ArgumentException("SQL Server requires a base connection string.", nameof(baseConnectionString));

                    // baseConnectionString should point to master or a server where you can create DBs
                    // You can also skip create/drop and just point to a precreated DB if you prefer.
                    var cs = $"{baseConnectionString};Database={dbName};";
                    return new EfTestDatabase(provider, dbName, cs, null);
                }

            case EfTestProvider.Postgres:
                {
                    if (string.IsNullOrWhiteSpace(baseConnectionString))
                        throw new ArgumentException("Postgres requires a base connection string.", nameof(baseConnectionString));

                    // baseConnectionString should point to a server; we’ll append Database=
                    var cs = $"{baseConnectionString};Database={dbName};";
                    return new EfTestDatabase(provider, dbName, cs, null);
                }

            default:
                throw new NotSupportedException($"Unknown provider: {provider}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (SharedConnection is not null)
        {
            await SharedConnection.DisposeAsync().ConfigureAwait(false);
        }
    }
}