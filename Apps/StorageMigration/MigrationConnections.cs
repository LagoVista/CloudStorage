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

    public static CassandraMigrationConnection Cassandra => new()
    {
        ContactPoints = (Optional("MIGRATION_CASSANDRA_HOSTS") ?? "localhost").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        Port = Int32.Parse(Optional("MIGRATION_CASSANDRA_PORT") ?? "19042", CultureInfo.InvariantCulture),
        UserName = Optional("MIGRATION_CASSANDRA_USERNAME") ?? "cassandra",
        Password = Optional("MIGRATION_CASSANDRA_PASSWORD") ?? "cassandra",
        Keyspace = Optional("MIGRATION_CASSANDRA_KEYSPACE") ?? "nuviot_cloudstorage_tests",
        LocalDataCenter = Optional("MIGRATION_CASSANDRA_DATACENTER") ?? "datacenter1",
        ReplicationFactor = Int32.Parse(Optional("MIGRATION_CASSANDRA_REPLICATION_FACTOR") ?? "1", CultureInfo.InvariantCulture)
    };

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
