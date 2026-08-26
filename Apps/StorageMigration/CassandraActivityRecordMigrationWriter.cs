using Cassandra;
using System.Text.RegularExpressions;

namespace LagoVista.StorageMigration;

public sealed class CassandraMigrationConnection
{
    public string[] ContactPoints { get; init; } = Array.Empty<string>();
    public int Port { get; init; } = 9042;
    public string UserName { get; init; } = String.Empty;
    public string Password { get; init; } = String.Empty;
    public string Keyspace { get; init; } = String.Empty;
    public string? LocalDataCenter { get; init; }
    public int ReplicationFactor { get; init; } = 1;
}

public sealed class CassandraActivityRecordMigrationWriter : IActivityRecordMigrationWriter, IDisposable
{
    private static readonly Regex CassandraIdentifier = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);
    private readonly Cluster _cluster;
    private readonly ISession _session;
    private readonly Dictionary<string, PreparedStatement> _inserts = new(StringComparer.OrdinalIgnoreCase);

    public CassandraActivityRecordMigrationWriter(CassandraMigrationConnection connection)
    {
        if (connection.ContactPoints.Length == 0) throw new ArgumentException("At least one Cassandra contact point is required.", nameof(connection));
        if (String.IsNullOrWhiteSpace(connection.Keyspace) || !CassandraIdentifier.IsMatch(connection.Keyspace)) throw new ArgumentException("A valid Cassandra keyspace is required.", nameof(connection));
        if (connection.ReplicationFactor <= 0) throw new ArgumentOutOfRangeException(nameof(connection));

        var builder = Cluster.Builder().AddContactPoints(connection.ContactPoints).WithPort(connection.Port);
        if (!String.IsNullOrWhiteSpace(connection.UserName)) builder = builder.WithCredentials(connection.UserName, connection.Password);
        if (!String.IsNullOrWhiteSpace(connection.LocalDataCenter)) builder = builder.WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy(connection.LocalDataCenter));
        _cluster = builder.Build();
        using (var bootstrap = _cluster.Connect()) bootstrap.Execute(new SimpleStatement(CreateKeyspaceCql(connection)));
        _session = _cluster.Connect(connection.Keyspace);
    }

    public async Task EnsureSchemaAsync(MigrationDefinition definition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fields = definition.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        var columns = definition.Fields.Select(field => $"{field.Name} {NormalizeType(field.Type)}").ToList();
        var partition = definition.Target.PartitionFields.ToList();
        if (UsesBuckets(definition)) { columns.Add("time_bucket text"); partition.Add("time_bucket"); }
        var ttl = definition.Target.RetentionSeconds ?? 0;
        var create = $@"CREATE TABLE IF NOT EXISTS {definition.Target.Table} (
    {String.Join(",\n    ", columns)},
    PRIMARY KEY (({String.Join(", ", partition)}), {definition.Target.TimeField}, {definition.Target.KeyField})
) WITH CLUSTERING ORDER BY ({definition.Target.TimeField} DESC, {definition.Target.KeyField} ASC)
AND default_time_to_live = {ttl}";
        await _session.ExecuteAsync(new SimpleStatement(create)).ConfigureAwait(false);
        foreach (var index in definition.Target.Indexes)
        {
            if (!fields.ContainsKey(index)) throw new InvalidOperationException($"Index field {index} is not declared.");
            await _session.ExecuteAsync(new SimpleStatement($"CREATE INDEX IF NOT EXISTS {IndexName(definition.Target.Table, index)} ON {definition.Target.Table} ({index}) USING 'sai'")).ConfigureAwait(false);
        }
        await ValidateSchemaAsync(definition, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteBatchAsync(MigrationDefinition definition, IReadOnlyList<IReadOnlyDictionary<string, object?>> records, CancellationToken cancellationToken = default)
    {
        if (records.Count == 0) return;
        var insert = await GetInsertAsync(definition).ConfigureAwait(false);
        var columns = OrderedColumns(definition);
        const int maxConcurrency = 16;
        for (var offset = 0; offset < records.Count; offset += maxConcurrency)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(maxConcurrency, records.Count - offset);
            var writes = new Task<RowSet>[count];
            for (var index = 0; index < count; index++)
            {
                var record = records[offset + index];
                var values = columns.Select(column => record.TryGetValue(column, out var value) ? value : null).ToArray();
                writes[index] = _session.ExecuteAsync(insert.Bind(values));
            }
            await Task.WhenAll(writes).ConfigureAwait(false);
        }
    }

    private async Task ValidateSchemaAsync(MigrationDefinition definition, CancellationToken cancellationToken)
    {
        var expectedColumns = BuildExpectedColumns(definition);
        var columnStatement = await _session.PrepareAsync(@"
SELECT column_name, type, kind, position
FROM system_schema.columns
WHERE keyspace_name = ? AND table_name = ?").ConfigureAwait(false);
        var columnRows = await _session.ExecuteAsync(columnStatement.Bind(_session.Keyspace, definition.Target.Table)).ConfigureAwait(false);
        var actualColumns = columnRows.ToDictionary(row => row.GetValue<string>("column_name"), row => new ExistingColumn(row.GetValue<string>("type"), row.GetValue<string>("kind"), row.GetValue<int>("position")), StringComparer.OrdinalIgnoreCase);
        foreach (var expected in expectedColumns)
        {
            if (!actualColumns.TryGetValue(expected.Name, out var actual)) throw new InvalidOperationException($"Cassandra migration target {definition.Target.Table} is missing required column {expected.Name}.");
            if (!String.Equals(NormalizeCqlType(actual.Type), NormalizeCqlType(expected.Type), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Cassandra migration target {definition.Target.Table}.{expected.Name} has type {actual.Type}; expected {expected.Type}.");
            if (!String.Equals(actual.Kind, expected.Kind, StringComparison.OrdinalIgnoreCase) || (expected.Kind != "regular" && actual.Position != expected.Position)) throw new InvalidOperationException($"Cassandra migration target {definition.Target.Table}.{expected.Name} has key shape {actual.Kind}[{actual.Position}]; expected {expected.Kind}[{expected.Position}].");
        }
        var tableStatement = await _session.PrepareAsync("SELECT default_time_to_live FROM system_schema.tables WHERE keyspace_name = ? AND table_name = ?").ConfigureAwait(false);
        var tableRows = await _session.ExecuteAsync(tableStatement.Bind(_session.Keyspace, definition.Target.Table)).ConfigureAwait(false);
        var tableRow = tableRows.SingleOrDefault() ?? throw new InvalidOperationException($"Cassandra migration target table {definition.Target.Table} was not found after schema preparation.");
        var actualTtl = tableRow.GetValue<int>("default_time_to_live");
        var expectedTtl = definition.Target.RetentionSeconds ?? 0;
        if (actualTtl != expectedTtl) throw new InvalidOperationException($"Cassandra migration target {definition.Target.Table} has default_time_to_live={actualTtl}; expected {expectedTtl}.");
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<PreparedStatement> GetInsertAsync(MigrationDefinition definition)
    {
        if (_inserts.TryGetValue(definition.Key, out var prepared)) return prepared;
        var columns = OrderedColumns(definition);
        prepared = await _session.PrepareAsync($"INSERT INTO {definition.Target.Table} ({String.Join(", ", columns)}) VALUES ({String.Join(", ", columns.Select(_ => "?"))})").ConfigureAwait(false);
        _inserts[definition.Key] = prepared;
        return prepared;
    }

    private static IReadOnlyList<ExpectedColumn> BuildExpectedColumns(MigrationDefinition definition)
    {
        var expected = new List<ExpectedColumn>();
        var fields = definition.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < definition.Target.PartitionFields.Count; index++) { var name = definition.Target.PartitionFields[index]; expected.Add(new ExpectedColumn(name, NormalizeType(fields[name].Type), "partition_key", index)); }
        if (UsesBuckets(definition)) expected.Add(new ExpectedColumn("time_bucket", "text", "partition_key", definition.Target.PartitionFields.Count));
        expected.Add(new ExpectedColumn(definition.Target.TimeField, NormalizeType(fields[definition.Target.TimeField].Type), "clustering", 0));
        expected.Add(new ExpectedColumn(definition.Target.KeyField, NormalizeType(fields[definition.Target.KeyField].Type), "clustering", 1));
        var keyFields = new HashSet<string>(definition.Target.PartitionFields, StringComparer.OrdinalIgnoreCase) { definition.Target.TimeField, definition.Target.KeyField };
        foreach (var field in definition.Fields) if (!keyFields.Contains(field.Name)) expected.Add(new ExpectedColumn(field.Name, NormalizeType(field.Type), "regular", -1));
        return expected;
    }

    private static IReadOnlyList<string> OrderedColumns(MigrationDefinition definition) { var columns = definition.Fields.Select(field => field.Name).ToList(); if (UsesBuckets(definition)) columns.Add("time_bucket"); return columns; }
    private static string CreateKeyspaceCql(CassandraMigrationConnection connection) => !String.IsNullOrWhiteSpace(connection.LocalDataCenter)
        ? $"CREATE KEYSPACE IF NOT EXISTS {connection.Keyspace} WITH replication = {{'class':'NetworkTopologyStrategy','{connection.LocalDataCenter.Replace("'", "''")}':{connection.ReplicationFactor}}}"
        : $"CREATE KEYSPACE IF NOT EXISTS {connection.Keyspace} WITH replication = {{'class':'SimpleStrategy','replication_factor':{connection.ReplicationFactor}}}";
    private static bool UsesBuckets(MigrationDefinition definition) => !String.Equals(definition.Target.Bucket, "All", StringComparison.OrdinalIgnoreCase);
    private static string IndexName(string table, string field) => $"{table}_{field}_sai_idx";
    private static string NormalizeType(string type) => type.ToLowerInvariant() switch { "text" or "boolean" or "int" or "bigint" or "decimal" or "timestamp" => type.ToLowerInvariant(), _ => throw new NotSupportedException($"Migration target CQL type '{type}' is not supported.") };
    private static string NormalizeCqlType(string type) => String.IsNullOrWhiteSpace(type) ? String.Empty : type.Replace(" ", String.Empty).ToLowerInvariant();

    public void Dispose() { _session.Dispose(); _cluster.Dispose(); }
    private sealed record ExistingColumn(string Type, string Kind, int Position);
    private sealed record ExpectedColumn(string Name, string Type, string Kind, int Position);
}
