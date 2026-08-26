using LagoVista;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core;
using LagoVista.Core.Models;

namespace EntityMigration202608;

internal sealed class EntityMigration202608Progress : IApplicationDataRecord
{
    public NormalizedId32 Id { get; set; }
    public EntityHeader Organization { get; set; }
    public UtcTimestamp CreationDate { get; set; }
    public UtcTimestamp LastUpdatedDate { get; set; }

    public string MigrationName { get; set; }
    public string Environment { get; set; }
    public string SourceDatabaseName { get; set; }
    public string TargetDatabaseName { get; set; }
    public string Status { get; set; }
    public int RunCount { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public List<EntityMigrationSourceProgress> Sources { get; set; } = new();
    public List<EntityMigrationRunSummary> Runs { get; set; } = new();
}

internal sealed class EntityMigrationSourceProgress
{
    public string SourceCollectionName { get; set; }
    public string ContinuationToken { get; set; }
    public bool Completed { get; set; }
    public long PagesRead { get; set; }
    public long DocumentsRead { get; set; }
    public long DocumentsWritten { get; set; }
    public long DocumentsExcluded { get; set; }
    public long DocumentsSkipped { get; set; }
    public long DocumentsFailed { get; set; }
}

internal sealed class EntityMigrationRunSummary
{
    public string RunId { get; set; }
    public string Operation { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
    public string Status { get; set; }
    public int BatchSize { get; set; }
    public int MaxPagesPerSource { get; set; }
    public long PagesRead { get; set; }
    public long DocumentsRead { get; set; }
    public long DocumentsWritten { get; set; }
    public long DocumentsFailed { get; set; }
    public string Error { get; set; }
}

internal sealed class MigrationProgressStore
{
    private const string MigrationName = "EntityMigration202608";
    private static readonly EntityHeader MigrationOrganization = EntityHeader.Create("ENTITYMIGRATION202608", "Entity Migration 202608");

    private readonly IApplicationDataStore _store;
    private readonly string _environment;
    private readonly IApplicationDataStorageSettings _settings;

    private MigrationProgressStore(string environment, IApplicationDataStorageSettings settings)
    {
        _environment = environment;
        _settings = settings;
        _store = new MongoApplicationDataStore(settings, new MongoStorageClientFactory(), EmptyServiceProvider.Instance);
    }

    public string DatabaseName => _settings.DatabaseName;

    public static MigrationProgressStore Create(string environment)
    {
        var settings = MigrationApplicationDataSettings.FromEnvironment(environment);
        return new MigrationProgressStore(environment, settings);
    }

    public async Task<EntityMigration202608Progress> LoadOrCreateAsync(
        string sourceDatabaseName,
        string targetDatabaseName,
        IEnumerable<string> sourceCollections,
        CancellationToken ct)
    {
        var existing = await FindAsync(ct).ConfigureAwait(false);
        if (existing != null)
        {
            ValidateIdentity(existing, sourceDatabaseName, targetDatabaseName);
            EnsureSources(existing, sourceCollections);
            return existing;
        }

        var record = new EntityMigration202608Progress
        {
            Id = NormalizedId32.Factory(),
            Organization = MigrationOrganization,
            MigrationName = MigrationName,
            Environment = _environment,
            SourceDatabaseName = sourceDatabaseName,
            TargetDatabaseName = targetDatabaseName,
            Status = "NotStarted"
        };
        EnsureSources(record, sourceCollections);
        await _store.InsertAsync(record, ct).ConfigureAwait(false);
        return record;
    }

    public Task SaveAsync(EntityMigration202608Progress progress, CancellationToken ct)
    {
        return _store.UpdateAsync(progress, ct);
    }

    public async Task DeleteAsync(CancellationToken ct)
    {
        var existing = await FindAsync(ct).ConfigureAwait(false);
        if (existing == null) return;
        await _store.DeleteAsync<EntityMigration202608Progress>(new StorageKey(existing.Id.Value, existing.Organization.Id), ct).ConfigureAwait(false);
    }

    public Task<EntityMigration202608Progress> GetAsync(CancellationToken ct) => FindAsync(ct);

    private async Task<EntityMigration202608Progress> FindAsync(CancellationToken ct)
    {
        var page = await _store.QueryAsync(new StorageQuery<EntityMigration202608Progress>()
            .Where(x => x.Organization.Id, StorageFilterOperator.Equal, MigrationOrganization.Id)
            .Where(x => x.Environment, StorageFilterOperator.Equal, _environment)
            .WithPage(new StoragePageRequest(1)), ct).ConfigureAwait(false);

        return page.Items.FirstOrDefault();
    }

    private static void ValidateIdentity(EntityMigration202608Progress progress, string sourceDatabaseName, string targetDatabaseName)
    {
        if (!String.Equals(progress.SourceDatabaseName, sourceDatabaseName, StringComparison.OrdinalIgnoreCase) ||
            !String.Equals(progress.TargetDatabaseName, targetDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Saved migration progress belongs to Cosmos '{progress.SourceDatabaseName}' -> Mongo '{progress.TargetDatabaseName}', " +
                $"but current settings resolve to Cosmos '{sourceDatabaseName}' -> Mongo '{targetDatabaseName}'. Reset or correct the settings before resuming.");
        }
    }

    private static void EnsureSources(EntityMigration202608Progress progress, IEnumerable<string> sourceCollections)
    {
        progress.Sources ??= new List<EntityMigrationSourceProgress>();
        foreach (var sourceCollection in sourceCollections)
        {
            if (!progress.Sources.Any(source => String.Equals(source.SourceCollectionName, sourceCollection, StringComparison.OrdinalIgnoreCase)))
                progress.Sources.Add(new EntityMigrationSourceProgress { SourceCollectionName = sourceCollection });
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();
        public object GetService(Type serviceType) => null;
    }

    /// <summary>
    /// The migration tracker uses the same Mongo server connection as entity storage.
    /// Application Data differs only by database name.
    /// </summary>
    private sealed class MigrationApplicationDataSettings : IApplicationDataStorageSettings
    {
        private readonly MongoDocumentStorageConnectionSettings _mongo;

        private MigrationApplicationDataSettings(MongoDocumentStorageConnectionSettings mongo, string databaseName)
        {
            _mongo = mongo ?? throw new ArgumentNullException(nameof(mongo));
            DatabaseName = databaseName;
        }

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
            var mongo = prefix == "PROD"
                ? TestConnections.ProductionMongoDocumentStorage
                : TestConnections.DevMongoDocumentStorage;

            var databaseName = Environment.GetEnvironmentVariable($"{prefix}_ApplicationDataStorage:DatabaseName")
                ?? Environment.GetEnvironmentVariable($"{prefix}_ApplicationDataStorage__DatabaseName")
                ?? Environment.GetEnvironmentVariable("ApplicationDataStorage:DatabaseName")
                ?? Environment.GetEnvironmentVariable("ApplicationDataStorage__DatabaseName");

            if (String.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException($"Missing {prefix}_ApplicationDataStorage:DatabaseName environment variable.");

            return new MigrationApplicationDataSettings(mongo, databaseName);
        }

        public string BuildConnectionString() => _mongo.BuildConnectionString();
    }
}
