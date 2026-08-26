using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.StorageProviders;
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

    public async Task<EntityMigration202608Progress> LoadOrCreateAsync(IEnumerable<string> sourceCollections, CancellationToken ct)
    {
        var existing = await FindAsync(ct).ConfigureAwait(false);
        if (existing != null)
        {
            EnsureSources(existing, sourceCollections);
            return existing;
        }

        var record = new EntityMigration202608Progress
        {
            Id = NormalizedId32.Factory(),
            Organization = MigrationOrganization,
            MigrationName = MigrationName,
            Environment = _environment,
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

    private sealed class MigrationApplicationDataSettings : IApplicationDataStorageSettings
    {
        private MigrationApplicationDataSettings()
        {
        }

        public IReadOnlyList<string> Hosts { get; private set; }
        public int Port { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string AuthenticationDatabase { get; private set; }
        public string DatabaseName { get; private set; }
        public string ReplicaSet { get; private set; }
        public bool UseTls { get; private set; }

        public static MigrationApplicationDataSettings FromEnvironment(string environment)
        {
            var prefix = String.Equals(environment, "prod", StringComparison.OrdinalIgnoreCase) ? "PROD" : "DEV";
            var mongoFallback = prefix == "PROD"
                ? LagoVista.CloudStorage.Utils.TestConnections.ProductionMongoDocumentStorage
                : LagoVista.CloudStorage.Utils.TestConnections.DevMongoDocumentStorage;

            var hosts = Read(prefix, "Hosts");
            var databaseName = Read(prefix, "DatabaseName");
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException($"Missing {prefix}_ApplicationDataStorage:DatabaseName environment variable.");

            return new MigrationApplicationDataSettings
            {
                Hosts = String.IsNullOrWhiteSpace(hosts)
                    ? mongoFallback.Hosts
                    : hosts.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).ToArray(),
                Port = Int32.TryParse(Read(prefix, "Port"), out var port) ? port : mongoFallback.Port,
                UserName = Read(prefix, "UserName") ?? mongoFallback.UserName,
                Password = Read(prefix, "Password") ?? mongoFallback.Password,
                AuthenticationDatabase = Read(prefix, "AuthenticationDatabase") ?? mongoFallback.AuthenticationDatabase ?? "admin",
                DatabaseName = databaseName,
                ReplicaSet = Read(prefix, "ReplicaSet") ?? mongoFallback.ReplicaSet,
                UseTls = Boolean.TryParse(Read(prefix, "UseTls"), out var useTls) ? useTls : mongoFallback.UseTls
            };
        }

        public string BuildConnectionString()
        {
            var settings = new MongoDocumentStorageConnectionSettings
            {
                Hosts = Hosts,
                Port = Port,
                UserName = UserName,
                Password = Password,
                AuthenticationDatabase = AuthenticationDatabase,
                DatabaseName = DatabaseName,
                ReplicaSet = ReplicaSet,
                UseTls = UseTls
            };
            return settings.BuildConnectionString();
        }

        private static string Read(string prefix, string name)
        {
            return Environment.GetEnvironmentVariable($"{prefix}_ApplicationDataStorage:{name}")
                ?? Environment.GetEnvironmentVariable($"{prefix}_ApplicationDataStorage__{name}")
                ?? Environment.GetEnvironmentVariable($"ApplicationDataStorage:{name}")
                ?? Environment.GetEnvironmentVariable($"ApplicationDataStorage__{name}");
        }
    }
}
