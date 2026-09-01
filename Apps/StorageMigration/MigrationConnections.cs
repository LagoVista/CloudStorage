using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using System.Globalization;

namespace LagoVista.StorageMigration;

public static class MigrationConnections
{
    private static IDefaultConnectionSettings? _defaultConnections;
    private static IS3ObjectStorageConnectionSettings? _s3ObjectStorage;
    private static ICassandraStorageSettings? _cassandra;
    private static IApplicationDataStorageSettings? _applicationDataStorage;

    public static string EnvironmentName { get; private set; } = "dev";

    public static IS3ObjectStorageConnectionSettings S3ObjectStorage =>
        _s3ObjectStorage ?? throw new InvalidOperationException("Migration connections have not been configured.");

    public static IApplicationDataStorageSettings ApplicationDataStorage =>
        _applicationDataStorage ?? throw new InvalidOperationException("Migration connections have not been configured.");

    public static void Configure(
        string environmentName,
        IDefaultConnectionSettings defaultConnections,
        IS3ObjectStorageConnectionSettings s3ObjectStorage,
        ICassandraStorageSettings cassandra,
        IApplicationDataStorageSettings applicationDataStorage)
    {
        EnvironmentName = NormalizeEnvironment(environmentName);
        _defaultConnections = defaultConnections ?? throw new ArgumentNullException(nameof(defaultConnections));
        _s3ObjectStorage = s3ObjectStorage ?? throw new ArgumentNullException(nameof(s3ObjectStorage));
        _cassandra = cassandra ?? throw new ArgumentNullException(nameof(cassandra));
        _applicationDataStorage = applicationDataStorage ?? throw new ArgumentNullException(nameof(applicationDataStorage));
    }

    public static string AzureTableConnectionString(string logicalConnection)
    {
        _ = logicalConnection;
        return AzureStorageConnectionString();
    }

    public static string AzureBlobConnectionString() => AzureStorageConnectionString();

    public static CassandraMigrationConnection Cassandra
    {
        get
        {
            var settings = _cassandra ?? throw new InvalidOperationException("Migration connections have not been configured.");
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

    private static string AzureStorageConnectionString()
    {
        var settings = _defaultConnections?.DefaultTableStorageSettings
            ?? throw new InvalidOperationException("Migration connections have not been configured.");

        if (String.IsNullOrWhiteSpace(settings.AccountId))
            throw new InvalidOperationException("DefaultTableStorage:Name is missing from resolved configuration.");
        if (String.IsNullOrWhiteSpace(settings.AccessKey))
            throw new InvalidOperationException("DefaultTableStorage:AccessKey is missing from resolved configuration.");

        return $"DefaultEndpointsProtocol=https;AccountName={settings.AccountId};AccountKey={settings.AccessKey}";
    }

    private static string NormalizeEnvironment(string value) =>
        String.Equals(value, "prod", StringComparison.OrdinalIgnoreCase) ? "live" : value.Trim().ToLowerInvariant();

    private static string? Optional(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (String.IsNullOrWhiteSpace(value) && OperatingSystem.IsWindows())
            value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
