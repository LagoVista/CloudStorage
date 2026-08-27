using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.Core;
using LagoVista.Core.Models;

namespace LagoVista.StorageMigration;

internal sealed class StorageMigrationStateRecord : IApplicationDataRecord
{
    public NormalizedId32 Id { get; set; }
    public EntityHeader? Organization { get; set; } = null;
    public UtcTimestamp CreationDate { get; set; }
    public UtcTimestamp LastUpdatedDate { get; set; }

    public string MigrationKey { get; set; } = String.Empty;
    public string DefinitionSha256 { get; set; } = String.Empty;
    public string State { get; set; } = "NotStarted";
    public int PassNumber { get; set; } = 1;
    public string? CurrentTable { get; set; }
    public string? HeadPartitionKey { get; set; }
    public string? HeadRowKey { get; set; }
    public long RecordsRead { get; set; }
    public long RecordsWritten { get; set; }
    public long RecordsFailed { get; set; }
    public long PriorPassRecordsRead { get; set; }
    public long PriorPassRecordsWritten { get; set; }
    public long PriorPassRecordsFailed { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public sealed class ApplicationDataMigrationStateStore : IMigrationStateStore
{
    private static readonly EntityHeader MigrationOrganization = EntityHeader.Create("STORAGEMIGRATION", "Storage Migration");
    private readonly IApplicationDataStore _store;

    private ApplicationDataMigrationStateStore(IApplicationDataStorageSettings settings)
    {
        _store = new MongoApplicationDataStore(settings, new MongoStorageClientFactory(), EmptyServiceProvider.Instance);
    }

    public static ApplicationDataMigrationStateStore Create(string environment)
    {
        var settings = MigrationApplicationDataSettings.FromEnvironment(environment);
        Console.WriteLine($"Migration state DB : {settings.DatabaseName}");
        Console.WriteLine("Migration state Mongo connection: direct external endpoint");
        return new ApplicationDataMigrationStateStore(settings);
    }

    public async Task<MigrationRunState?> GetAsync(string migrationKey, CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(migrationKey, cancellationToken).ConfigureAwait(false);
        return record == null ? null : ToState(record);
    }

    public async Task UpsertAsync(MigrationRunState state, CancellationToken cancellationToken = default)
    {
        var existing = await FindAsync(state.MigrationKey, cancellationToken).ConfigureAwait(false);
        if (existing == null)
        {
            existing = new StorageMigrationStateRecord
            {
                Id = NormalizedId32.Factory(),
                Organization = MigrationOrganization,
                MigrationKey = state.MigrationKey
            };
            Apply(state, existing);
            await _store.InsertAsync(existing, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Apply(state, existing);
            await _store.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<StorageMigrationStateRecord?> FindAsync(string migrationKey, CancellationToken ct)
    {
        var page = await _store.QueryAsync(new StorageQuery<StorageMigrationStateRecord>()
            .Where(x => x.Organization!.Id, StorageFilterOperator.Equal, MigrationOrganization.Id)
            .Where(x => x.MigrationKey, StorageFilterOperator.Equal, migrationKey)
            .WithPage(new StoragePageRequest(1)), ct).ConfigureAwait(false);
        return page.Items.FirstOrDefault();
    }

    private static void Apply(MigrationRunState state, StorageMigrationStateRecord record)
    {
        record.DefinitionSha256 = state.DefinitionSha256;
        record.State = state.State;
        record.PassNumber = state.PassNumber;
        record.CurrentTable = state.CurrentTable;
        record.HeadPartitionKey = state.HeadPartitionKey;
        record.HeadRowKey = state.HeadRowKey;
        record.RecordsRead = state.RecordsRead;
        record.RecordsWritten = state.RecordsWritten;
        record.RecordsFailed = state.RecordsFailed;
        record.PriorPassRecordsRead = state.PriorPassRecordsRead;
        record.PriorPassRecordsWritten = state.PriorPassRecordsWritten;
        record.PriorPassRecordsFailed = state.PriorPassRecordsFailed;
        record.CompletedDate = state.CompletedDate;
    }

    private static MigrationRunState ToState(StorageMigrationStateRecord record) => new()
    {
        Id = record.Id.Value,
        MigrationKey = record.MigrationKey,
        DefinitionSha256 = record.DefinitionSha256,
        State = record.State,
        PassNumber = record.PassNumber,
        CurrentTable = record.CurrentTable,
        HeadPartitionKey = record.HeadPartitionKey,
        HeadRowKey = record.HeadRowKey,
        RecordsRead = record.RecordsRead,
        RecordsWritten = record.RecordsWritten,
        RecordsFailed = record.RecordsFailed,
        PriorPassRecordsRead = record.PriorPassRecordsRead,
        PriorPassRecordsWritten = record.PriorPassRecordsWritten,
        PriorPassRecordsFailed = record.PriorPassRecordsFailed,
        CreationDate = DateTime.UtcNow,
        LastUpdatedDate = DateTime.UtcNow,
        CompletedDate = record.CompletedDate
    };

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();
        public object? GetService(Type serviceType) => null;
    }

    private sealed class MigrationApplicationDataSettings : IApplicationDataStorageSettings
    {
        private readonly MongoDocumentStorageConnectionSettings _mongo;
        private MigrationApplicationDataSettings(MongoDocumentStorageConnectionSettings mongo, string databaseName) { _mongo = mongo; DatabaseName = databaseName; }
        public IReadOnlyList<string> Hosts => _mongo.Hosts;
        public int Port => _mongo.Port;
        public string UserName => _mongo.UserName;
        public string Password => _mongo.Password;
        public string AuthenticationDatabase => _mongo.AuthenticationDatabase;
        public string DatabaseName { get; }
        public string ReplicaSet => _mongo.ReplicaSet;
        public bool UseTls => _mongo.UseTls;

        public static MigrationApplicationDataSettings FromEnvironment(string environment)
        {
            var prefix = String.Equals(environment, "prod", StringComparison.OrdinalIgnoreCase) ? "PROD" : "DEV";
            var mongo = prefix == "PROD" ? LagoVista.CloudStorage.Utils.TestConnections.ProductionMongoDocumentStorage : LagoVista.CloudStorage.Utils.TestConnections.DevMongoDocumentStorage;
            var databaseName = Environment.GetEnvironmentVariable($"{prefix}_ApplicationDataStorage:DatabaseName")
                ?? Environment.GetEnvironmentVariable($"{prefix}_ApplicationDataStorage__DatabaseName")
                ?? Environment.GetEnvironmentVariable("ApplicationDataStorage:DatabaseName")
                ?? Environment.GetEnvironmentVariable("ApplicationDataStorage__DatabaseName");
            if (String.IsNullOrWhiteSpace(databaseName)) throw new InvalidOperationException($"Missing {prefix}_ApplicationDataStorage:DatabaseName environment variable.");
            return new MigrationApplicationDataSettings(mongo, databaseName);
        }

        public string BuildConnectionString()
        {
            if (Hosts == null || Hosts.Count != 1 || String.IsNullOrWhiteSpace(Hosts[0])) throw new InvalidOperationException("StorageMigration direct Mongo mode requires exactly one externally reachable Mongo host.");
            var direct = new MongoDocumentStorageConnectionSettings
            {
                Hosts = Hosts, Port = Port, UserName = UserName, Password = Password,
                AuthenticationDatabase = AuthenticationDatabase, DatabaseName = DatabaseName,
                ReplicaSet = null, UseTls = UseTls
            };
            return direct.BuildConnectionString() + "&directConnection=true";
        }
    }
}
