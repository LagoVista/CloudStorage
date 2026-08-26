using LagoVista.CloudStorage.Utils;
using System.Globalization;

namespace LagoVista.StorageMigration;

public static class MigrationConnections
{
    public static string AzureTableConnectionString(string logicalConnection)
    {
        var prefix = logicalConnection?.Trim().ToLowerInvariant() switch
        {
            "access-log" => "MIGRATION_AZURE_ACCESS_LOG",
            "user-storage" => "MIGRATION_AZURE_USER_STORAGE",
            _ => "MIGRATION_AZURE_TABLE"
        };

        var accountId = Optional($"{prefix}_ACCOUNT_ID") ?? Required("MIGRATION_AZURE_TABLE_ACCOUNT_ID");
        var accessKey = Optional($"{prefix}_ACCESS_KEY") ?? Required("MIGRATION_AZURE_TABLE_ACCESS_KEY");
        return $"DefaultEndpointsProtocol=https;AccountName={accountId};AccountKey={accessKey}";
    }

    public static CassandraMigrationConnection Cassandra
    {
        get
        {
            var settings = String.Equals(EnvironmentName, "prod", StringComparison.OrdinalIgnoreCase)
                ? TestConnections.ProductionCassandraStorage
                : TestConnections.DevCassandraStorage;

            return new CassandraMigrationConnection
            {
                ContactPoints = settings.ContactPoints.ToArray(),
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                Keyspace = settings.Keyspace,
                LocalDataCenter = settings.LocalDataCenter,
                ReplicationFactor = Int32.Parse(Optional("MIGRATION_CASSANDRA_REPLICATION_FACTOR") ?? "1", CultureInfo.InvariantCulture)
            };
        }
    }

    public static string EnvironmentName => String.Equals(Optional("MIGRATION_ENVIRONMENT"), "prod", StringComparison.OrdinalIgnoreCase) ? "prod" : "dev";

    private static string Required(string name)
    {
        var value = Optional(name);
        if (String.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"Missing required migration environment variable {name}.");
        return value;
    }

    private static string? Optional(string name)
    {
        var value = System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
            ?? System.Environment.GetEnvironmentVariable(name);
        return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
