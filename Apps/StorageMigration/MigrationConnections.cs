using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Utils;
using System.Globalization;

namespace LagoVista.StorageMigration;

public static class MigrationConnections
{
    public static string AzureTableConnectionString(string logicalConnection)
    {
        var settings = AzureStorageSettings(logicalConnection);

        if (String.IsNullOrWhiteSpace(settings.AccountId))
            throw new InvalidOperationException($"Missing {EnvironmentName.ToUpperInvariant()} table storage account id.");
        if (String.IsNullOrWhiteSpace(settings.AccessKey))
            throw new InvalidOperationException($"Missing {EnvironmentName.ToUpperInvariant()} table storage access key.");

        return $"DefaultEndpointsProtocol=https;AccountName={settings.AccountId};AccountKey={settings.AccessKey}";
    }

    public static string AzureBlobConnectionString()
    {
        var settings = AzureStorageSettings();

        if (String.IsNullOrWhiteSpace(settings.AccountId))
            throw new InvalidOperationException($"Missing {EnvironmentName.ToUpperInvariant()} Azure storage account id.");
        if (String.IsNullOrWhiteSpace(settings.AccessKey))
            throw new InvalidOperationException($"Missing {EnvironmentName.ToUpperInvariant()} Azure storage access key.");

        return $"DefaultEndpointsProtocol=https;AccountName={settings.AccountId};AccountKey={settings.AccessKey}";
    }

    public static IS3ObjectStorageConnectionSettings S3ObjectStorage =>
        String.Equals(EnvironmentName, "prod", StringComparison.OrdinalIgnoreCase)
            ? TestConnections.ProductionS3ObjectStorage
            : TestConnections.DevS3ObjectStorage;

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

    public static string EnvironmentName
    {
        get
        {
            var configured = Optional("MIGRATION_ENVIRONMENT");
            if (String.IsNullOrWhiteSpace(configured) || String.Equals(configured, "dev", StringComparison.OrdinalIgnoreCase))
                return "dev";
            if (String.Equals(configured, "prod", StringComparison.OrdinalIgnoreCase))
                return "prod";
            throw new InvalidOperationException("MIGRATION_ENVIRONMENT must be 'dev' or 'prod'.");
        }
    }

    private static LagoVista.Core.Models.ConnectionSettings AzureStorageSettings(string? logicalConnection = null)
    {
        // Logical source names identify migration definitions, not Azure accounts.
        // All Table Storage migrations use the shared CloudStorage DEV/PROD account.
        // Keep known names explicit so a typo in a migration definition fails fast.
        switch (logicalConnection?.Trim().ToLowerInvariant())
        {
            case null:
            case "":
            case "access-log":
            case "marketing-storage":
            case "billing-storage":
            case "user-storage":
            case "ai-ml-table-storage":
                return DefaultAzureStorageSettings();
            default:
                throw new InvalidOperationException($"Unknown migration Azure source connection '{logicalConnection}'.");
        }
    }

    private static LagoVista.Core.Models.ConnectionSettings DefaultAzureStorageSettings() =>
        String.Equals(EnvironmentName, "prod", StringComparison.OrdinalIgnoreCase)
            ? TestConnections.ProductionTableStorageDB
            : TestConnections.DevTableStorageDB;

    private static string? Optional(string name)
    {
        // Explicit per-process operator settings must override saved user/machine defaults.
        // This is critical when switching between DEV and PROD in PowerShell.
        var value = System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
            ?? System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
            ?? System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
        return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
