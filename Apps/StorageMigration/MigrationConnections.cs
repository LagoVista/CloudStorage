using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Utils;
using System.Globalization;

namespace LagoVista.StorageMigration;

public static class MigrationConnections
{
    public static string AzureTableConnectionString(string logicalConnection)
    {
        var settings = AzureStorageSettings();

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

    public static string EnvironmentName => String.Equals(Optional("MIGRATION_ENVIRONMENT"), "prod", StringComparison.OrdinalIgnoreCase) ? "prod" : "dev";

    private static LagoVista.Core.Models.ConnectionSettings AzureStorageSettings() =>
        String.Equals(EnvironmentName, "prod", StringComparison.OrdinalIgnoreCase)
            ? TestConnections.ProductionTableStorageDB
            : TestConnections.DevTableStorageDB;

    private static string? Optional(string name)
    {
        var value = System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
            ?? System.Environment.GetEnvironmentVariable(name);
        return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
