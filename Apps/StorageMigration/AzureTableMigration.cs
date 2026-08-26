using Azure.Data.Tables;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LagoVista.StorageMigration;

public sealed class AzureTableMigrationSource
{
    private readonly TableServiceClient _serviceClient;
    public AzureTableMigrationSource(string connectionString) => _serviceClient = new TableServiceClient(connectionString ?? throw new ArgumentNullException(nameof(connectionString)));

    public async Task<IReadOnlyList<string>> ResolveTablesAsync(MigrationDefinition definition, CancellationToken cancellationToken = default)
    {
        if (!String.IsNullOrWhiteSpace(definition.Source.TableName)) return new[] { definition.Source.TableName };
        var pattern = new Regex(definition.Source.TablePattern, RegexOptions.CultureInvariant);
        var tables = new List<string>();
        await foreach (var table in _serviceClient.QueryAsync(cancellationToken: cancellationToken)) if (pattern.IsMatch(table.Name)) tables.Add(table.Name);
        tables.Sort(StringComparer.OrdinalIgnoreCase);
        return tables.AsReadOnly();
    }

    public async Task<long> CountAsync(MigrationDefinition definition, CancellationToken cancellationToken = default)
    {
        long count = 0;
        foreach (var tableName in await ResolveTablesAsync(definition, cancellationToken).ConfigureAwait(false))
        {
            var table = _serviceClient.GetTableClient(tableName);
            await foreach (var _ in table.QueryAsync<TableEntity>(select: new[] { "PartitionKey", "RowKey" }, cancellationToken: cancellationToken)) count++;
        }
        return count;
    }

    public async IAsyncEnumerable<TableEntity> ReadAsync(string tableName, string? afterPartitionKey, string? afterRowKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var table = _serviceClient.GetTableClient(tableName);
        var passedHead = String.IsNullOrWhiteSpace(afterPartitionKey) && String.IsNullOrWhiteSpace(afterRowKey);
        await foreach (var row in table.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            if (!passedHead)
            {
                var partitionComparison = StringComparer.Ordinal.Compare(row.PartitionKey, afterPartitionKey);
                var rowComparison = StringComparer.Ordinal.Compare(row.RowKey, afterRowKey);
                if (partitionComparison < 0 || (partitionComparison == 0 && rowComparison <= 0)) continue;
                passedHead = true;
            }
            yield return row;
        }
    }
}

public sealed class AzureTableRecordMapper
{
    public IReadOnlyDictionary<string, object?> Map(MigrationDefinition definition, string sourceTable, TableEntity source)
    {
        var target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in definition.Fields)
        {
            var value = ResolveField(field, sourceTable, source);
            if (field.Required && IsMissing(value)) throw new InvalidOperationException($"Source {sourceTable}/{source.PartitionKey}/{source.RowKey} did not produce required target field {field.Name}.");
            target[field.Name] = ConvertTargetValue(field.Type, value);
        }
        target["time_bucket"] = CreateBucket(definition.Target.Bucket, RequireDate(target[definition.Target.TimeField]));
        return target;
    }

    private static object? ResolveField(MigrationFieldDefinition field, string sourceTable, TableEntity source)
    {
        if (String.Equals(field.Transform, "stable-id", StringComparison.OrdinalIgnoreCase))
        {
            var text = String.Join("|", (field.Sources ?? new()).Select(name => SourceValue(name, sourceTable, source)?.ToString() ?? String.Empty));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).Substring(0, 32).ToLowerInvariant();
        }
        if (String.Equals(field.Transform, "first-date", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var name in field.Sources ?? new()) if (TryDate(SourceValue(name, sourceTable, source), out var date)) return date;
            return null;
        }
        if (String.Equals(field.Transform, "anonymous-org-id", StringComparison.OrdinalIgnoreCase))
        {
            var value = SourceValue(field.Source ?? String.Empty, sourceTable, source)?.ToString();
            return String.IsNullOrWhiteSpace(value) || value == "?" ? "anonymous" : value;
        }
        if (String.Equals(field.Transform, "anonymous-org-name", StringComparison.OrdinalIgnoreCase))
        {
            var value = SourceValue(field.Source ?? String.Empty, sourceTable, source)?.ToString();
            return String.IsNullOrWhiteSpace(value) || value == "?" ? "Anonymous" : value;
        }
        if (!String.IsNullOrWhiteSpace(field.Transform)) throw new NotSupportedException($"Migration transform '{field.Transform}' is not supported.");
        return String.IsNullOrWhiteSpace(field.Source) ? null : SourceValue(field.Source, sourceTable, source);
    }

    private static object? SourceValue(string name, string sourceTable, TableEntity source)
    {
        if (String.Equals(name, "$table", StringComparison.OrdinalIgnoreCase)) return sourceTable;
        if (String.Equals(name, "$timestamp", StringComparison.OrdinalIgnoreCase)) return source.Timestamp;
        if (String.Equals(name, "PartitionKey", StringComparison.OrdinalIgnoreCase)) return source.PartitionKey;
        if (String.Equals(name, "RowKey", StringComparison.OrdinalIgnoreCase)) return source.RowKey;
        return source.TryGetValue(name, out var value) ? value : null;
    }

    private static object? ConvertTargetValue(string type, object? value)
    {
        if (value == null) return null;
        return type.ToLowerInvariant() switch
        {
            "text" => Convert.ToString(value, CultureInfo.InvariantCulture),
            "boolean" => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
            "int" => Convert.ToInt32(value, CultureInfo.InvariantCulture),
            "bigint" => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            "decimal" => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
            "timestamp" => RequireDate(value),
            _ => throw new NotSupportedException($"Migration target CQL type '{type}' is not supported by the mapper.")
        };
    }

    private static bool TryDate(object? value, out DateTimeOffset date)
    {
        if (value is DateTimeOffset dto) { date = dto.ToUniversalTime(); return true; }
        if (value is DateTime dt) { if (dt.Kind == DateTimeKind.Unspecified) dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc); date = new DateTimeOffset(dt.ToUniversalTime()); return true; }
        if (value is string text && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)) { date = parsed; return true; }
        date = default; return false;
    }

    private static DateTimeOffset RequireDate(object? value) => TryDate(value, out var date) ? date : throw new InvalidOperationException($"Value '{value}' is not a valid migration timestamp.");
    private static string? CreateBucket(string bucket, DateTimeOffset value) => bucket.ToLowerInvariant() switch
    {
        "all" => null,
        "month" => value.UtcDateTime.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        "quarter" => $"{value.UtcDateTime:yyyy}-Q{((value.UtcDateTime.Month - 1) / 3) + 1}",
        "year" => value.UtcDateTime.ToString("yyyy", CultureInfo.InvariantCulture),
        _ => throw new NotSupportedException($"Migration bucket '{bucket}' is not supported.")
    };
    private static bool IsMissing(object? value) => value == null || (value is string text && String.IsNullOrWhiteSpace(text));
}
