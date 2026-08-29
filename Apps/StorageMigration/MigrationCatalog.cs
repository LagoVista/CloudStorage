using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LagoVista.StorageMigration;

public sealed class MigrationCatalog
{
    private readonly string _definitionDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MigrationCatalog(string definitionDirectory) => _definitionDirectory = definitionDirectory ?? throw new ArgumentNullException(nameof(definitionDirectory));

    public IReadOnlyList<MigrationDefinition> LoadAll()
    {
        if (!Directory.Exists(_definitionDirectory)) return Array.Empty<MigrationDefinition>();
        return Directory.GetFiles(_definitionDirectory, "*.json")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(Load).ToList().AsReadOnly();
    }

    public MigrationDefinition LoadByKey(string key) => LoadAll().SingleOrDefault(x => String.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Migration definition '{key}' was not found in {_definitionDirectory}.");

    public string DefinitionSha256(MigrationDefinition definition)
    {
        var canonical = JsonSerializer.Serialize(definition, _jsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static IReadOnlyList<string> Validate(MigrationDefinition definition)
    {
        var errors = new List<string>();
        if (String.IsNullOrWhiteSpace(definition.Key)) errors.Add("key is required.");
        if (String.IsNullOrWhiteSpace(definition.Source.Connection)) errors.Add("source.connection is required.");
        if (String.IsNullOrWhiteSpace(definition.Source.TableName) && String.IsNullOrWhiteSpace(definition.Source.TablePattern)) errors.Add("source.tableName or source.tablePattern is required.");
        if (!String.IsNullOrWhiteSpace(definition.Source.TablePattern)) { try { _ = new Regex(definition.Source.TablePattern); } catch (ArgumentException ex) { errors.Add($"source.tablePattern is invalid: {ex.Message}"); } }
        if (String.IsNullOrWhiteSpace(definition.Target.Table)) errors.Add("target.table is required.");
        if (definition.Target.PartitionFields.Count == 0) errors.Add("target.partitionFields requires at least one field.");
        if (!new[] { "All", "Month", "Quarter", "Year" }.Contains(definition.Target.Bucket, StringComparer.OrdinalIgnoreCase)) errors.Add($"target.bucket '{definition.Target.Bucket}' is not supported.");
        if (definition.Target.RetentionSeconds.HasValue && definition.Target.RetentionSeconds.Value <= 0) errors.Add("target.retentionSeconds must be greater than zero when supplied.");

        var fieldNames = new HashSet<string>(definition.Fields.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var required in definition.Target.PartitionFields.Append(definition.Target.KeyField).Append(definition.Target.TimeField)) if (!fieldNames.Contains(required)) errors.Add($"target field '{required}' is not declared in fields.");
        foreach (var index in definition.Target.Indexes) if (!fieldNames.Contains(index)) errors.Add($"target index field '{index}' is not declared in fields.");
        foreach (var duplicate in definition.Fields.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1)) errors.Add($"field '{duplicate.Key}' is declared more than once.");
        return errors.AsReadOnly();
    }

    private MigrationDefinition Load(string path)
    {
        var definition = JsonSerializer.Deserialize<MigrationDefinition>(File.ReadAllText(path), _jsonOptions)
            ?? throw new InvalidOperationException($"Migration definition {path} could not be parsed.");
        var errors = Validate(definition);
        if (errors.Count > 0) throw new InvalidOperationException($"Migration definition {path} is invalid:{Environment.NewLine}- {String.Join(Environment.NewLine + "- ", errors)}");
        return definition;
    }
}
