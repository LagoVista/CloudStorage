using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LagoVista.StorageMigration;

/// <summary>
/// Business-model-free Azure Table to Mongo ApplicationData writer.
/// All writes are $setOnInsert by stable _id, so newer runtime records and
/// deletion tombstones always win over historical rows.
/// </summary>
public sealed class MongoApplicationDataMigrationWriter : IActivityRecordMigrationWriter, IDisposable
{
    private static readonly Regex CollectionNamePattern = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);
    private readonly IMongoDatabase _database;
    private readonly string _organizationId;
    private readonly string _organizationName;

    public MongoApplicationDataMigrationWriter(
        IMongoDatabase database, string ownerOrganizationId, string ownerOrganizationName)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _organizationId = !String.IsNullOrWhiteSpace(ownerOrganizationId) ? ownerOrganizationId
            : throw new ArgumentException("Mongo target owner organization Id is required.", nameof(ownerOrganizationId));
        _organizationName = !String.IsNullOrWhiteSpace(ownerOrganizationName) ? ownerOrganizationName
            : throw new ArgumentException("Mongo target owner organization name is required.", nameof(ownerOrganizationName));
    }

    public static MongoApplicationDataMigrationWriter FromEnvironment(MigrationDefinition definition, string environment)
    {
        if (!IsMongo(definition)) throw new ArgumentException("Expected a Mongo ApplicationData target.", nameof(definition));
        var id = RequiredVariable(definition.Target.OwnerOrganizationIdVariable);
        var name = RequiredVariable(definition.Target.OwnerOrganizationNameVariable);
        var config = ApplicationDataMigrationStateStore.GetTargetMongoConnection(environment);
        var client = new MongoClient(config.ConnectionString);
        return new MongoApplicationDataMigrationWriter(client.GetDatabase(config.DatabaseName), id, name);
    }

    private static string RequiredVariable(string? variable)
    {
        if (String.IsNullOrWhiteSpace(variable))
            throw new InvalidOperationException("Mongo definition requires an owner organization environment variable.");
        var value = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User)
            ?? Environment.GetEnvironmentVariable(variable);
        if (String.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Missing required Mongo target owner environment variable: " + variable);
        return value.Trim();
    }

    public async Task EnsureSchemaAsync(MigrationDefinition definition, CancellationToken cancellationToken = default)
    {
        Validate(definition);
        var collection = _database.GetCollection<BsonDocument>(definition.Target.Table);
        // Runtime ApplicationData always creates the owner-scope index.
        var fields = new HashSet<string>(definition.Target.Indexes, StringComparer.Ordinal);
        fields.Add("Organization.Id");
        var indexes = fields.Select(field => new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending(field),
            new CreateIndexOptions { Name = "ix_" + field.Replace('.', '_').TrimStart('_').ToLowerInvariant() }))
            .ToArray();
        if (indexes.Length > 0)
            await collection.Indexes.CreateManyAsync(indexes, cancellationToken)
                .ConfigureAwait(false);
    }

    public async Task WriteBatchAsync(MigrationDefinition definition,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> records, CancellationToken cancellationToken = default)
    {
        Validate(definition);
        if (records.Count == 0) return;
        var collection = _database.GetCollection<BsonDocument>(definition.Target.Table);
        const int maxConcurrency = 16;
        for (var offset = 0; offset < records.Count; offset += maxConcurrency)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = records.Skip(offset).Take(maxConcurrency).ToArray();
            var tasks = current.Select(row =>
            {
                var document = BuildDocument(definition, row);
                var id = document["_id"];
                return collection.UpdateOneAsync(
                    new BsonDocument("_id", id),
                    new BsonDocument("$setOnInsert", document),
                    new UpdateOptions { IsUpsert = true }, cancellationToken);
            }).ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <summary>Pure mapping projection; tests can assert runtime-compatible BSON without a database.</summary>
    public BsonDocument BuildDocument(MigrationDefinition definition, IReadOnlyDictionary<string, object?> fields)
    {
        Validate(definition);
        if (!fields.TryGetValue(definition.Target.KeyField, out var rawId) ||
            String.IsNullOrWhiteSpace(rawId?.ToString()))
            throw new InvalidOperationException("Mongo record is missing target key " + definition.Target.KeyField);
        if (!fields.TryGetValue("CreationDate", out var created) || created == null)
            throw new InvalidOperationException("Mongo ApplicationData record requires CreationDate.");
        if (!fields.TryGetValue("LastUpdatedDate", out var updated) || updated == null)
            throw new InvalidOperationException("Mongo ApplicationData record requires LastUpdatedDate.");

        var id = rawId.ToString()!;
        var document = new BsonDocument
        {
            { "_id", id },
            { "Organization", new BsonDocument
                {
                    { "Id", _organizationId },
                    { "Text", _organizationName }
                }
            },
            { "_storageVersion", Guid.NewGuid().ToString("N") }
        };
        foreach (var field in definition.Fields)
        {
            if (String.Equals(field.Name, definition.Target.KeyField, StringComparison.Ordinal)) continue;
            if (String.Equals(field.Name, "Organization", StringComparison.Ordinal) ||
                String.Equals(field.Name, "_id", StringComparison.Ordinal) ||
                String.Equals(field.Name, "_storageVersion", StringComparison.Ordinal))
                throw new InvalidOperationException("Mongo migration field is provider-owned: " + field.Name);
            if (!fields.TryGetValue(field.Name, out var value)) continue;
            document.Add(field.Name, ToBsonValue(field.Name, value));
        }
        return document;
    }

    private static BsonValue ToBsonValue(string fieldName, object? value)
    {
        if (value == null) return BsonNull.Value;
        if (value is DateTimeOffset dto)
            return String.Equals(fieldName, "CreationDate", StringComparison.Ordinal) ||
                String.Equals(fieldName, "LastUpdatedDate", StringComparison.Ordinal)
                ? new BsonString(dto.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
                : new BsonDateTime(dto.UtcDateTime);
        if (value is DateTime dt)
            return ToBsonValue(fieldName, new DateTimeOffset(dt.Kind == DateTimeKind.Utc
                ? dt : dt.ToUniversalTime()));
        return BsonValue.Create(value);
    }

    private static void Validate(MigrationDefinition definition)
    {
        if (!IsMongo(definition)) throw new ArgumentException("Expected mongo-application target.", nameof(definition));
        if (String.IsNullOrWhiteSpace(definition.Target.Table) ||
            !CollectionNamePattern.IsMatch(definition.Target.Table))
            throw new InvalidOperationException("Invalid Mongo target collection name.");
        if (definition.Fields.Any(f => f.Name == "Organization" || f.Name == "_storageVersion"))
            throw new InvalidOperationException("Mongo target definition cannot supply provider-owned fields.");
    }

    private static bool IsMongo(MigrationDefinition definition) =>
        definition != null && String.Equals(definition.Target.Type, "mongo-application", StringComparison.OrdinalIgnoreCase);

    public void Dispose() { /* MongoClient owns pooled connections for the command process. */ }
}
