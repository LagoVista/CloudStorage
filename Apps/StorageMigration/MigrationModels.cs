namespace LagoVista.StorageMigration;

public sealed class MigrationDefinition
{
    public string Key { get; set; } = String.Empty;
    public string DisplayName { get; set; } = String.Empty;
    public string Description { get; set; } = String.Empty;
    public AzureTableSourceDefinition Source { get; set; } = new();
    public CassandraTargetDefinition Target { get; set; } = new();
    public List<MigrationFieldDefinition> Fields { get; set; } = new();
}

public sealed class AzureTableSourceDefinition
{
    public string Type { get; set; } = "azure-table";
    public string Connection { get; set; } = String.Empty;
    public string TableName { get; set; } = String.Empty;
    public string TablePattern { get; set; } = String.Empty;
}

public sealed class CassandraTargetDefinition
{
    public string Type { get; set; } = "cassandra-activity";
    public string Table { get; set; } = String.Empty;
    public string KeyField { get; set; } = "id";
    public string TimeField { get; set; } = "creation_date";
    public string Bucket { get; set; } = "All";
    public List<string> PartitionFields { get; set; } = new();
    public List<string> Indexes { get; set; } = new();
    public int? RetentionSeconds { get; set; }
}

public sealed class MigrationFieldDefinition
{
    public string Name { get; set; } = String.Empty;
    public string Type { get; set; } = "text";
    public string? Source { get; set; }
    public List<string>? Sources { get; set; }
    public string? Transform { get; set; }
    public bool Required { get; set; }
}

public sealed class MigrationRunState
{
    public string Id { get; set; } = String.Empty;
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
    public DateTime CreationDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public interface IMigrationStateStore
{
    Task<MigrationRunState?> GetAsync(string migrationKey, CancellationToken cancellationToken = default);
    Task UpsertAsync(MigrationRunState state, CancellationToken cancellationToken = default);
}

public interface IActivityRecordMigrationWriter
{
    Task EnsureSchemaAsync(MigrationDefinition definition, CancellationToken cancellationToken = default);
    Task WriteBatchAsync(MigrationDefinition definition, IReadOnlyList<IReadOnlyDictionary<string, object?>> records, CancellationToken cancellationToken = default);
}
