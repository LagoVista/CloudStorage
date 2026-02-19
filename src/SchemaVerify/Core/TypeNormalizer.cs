namespace SchemaVerify.Core;

public readonly record struct NormalizedType(
    string Family,
    int? MaxLength,
    int? Precision,
    int? Scale,
    bool CompareMaxLength,
    bool ComparePrecisionScale);

public static class TypeNormalizer
{
    public static NormalizedType Normalize(string provider, ColumnModel c)
    {
        var st = (c.StoreType ?? string.Empty).Trim().ToLowerInvariant();
        var family = MapFamily(provider, st);

        // Default: compare facets only for appropriate families.
        var compareLen = family is "string" or "binary";
        var comparePrecScale = family is "decimal";

        return new NormalizedType(
            Family: family,
            MaxLength: c.MaxLength,
            Precision: c.Precision,
            Scale: c.Scale,
            CompareMaxLength: compareLen,
            ComparePrecisionScale: comparePrecScale);
    }

    private static string MapFamily(string provider, string storeType)
    {
        // Provider is used as a hint when type names overlap.
        // Keep this conservative; you can expand once you see real-world drift.

        if (string.IsNullOrWhiteSpace(storeType)) return "unknown";

        // Strings
        if (storeType.Contains("char") || storeType.Contains("text") || storeType.Contains("varchar") || storeType.Contains("nvarchar")) return "string";

        // UUID / uniqueidentifier
        if (storeType.Contains("uuid") || storeType.Contains("uniqueidentifier")) return "uuid";

        // Booleans
        if (storeType == "bit" || storeType.Contains("boolean") || storeType == "bool") return "bool";

        // Integers
        if (storeType.Contains("bigint")) return "int64";
        if (storeType.Contains("smallint")) return "int16";
        if (storeType.Contains("int") || storeType.Contains("integer")) return "int32";

        // Decimal / numeric
        if (storeType.Contains("numeric") || storeType.Contains("decimal") || storeType.Contains("money")) return "decimal";

        // Floating
        if (storeType.Contains("real") || storeType.Contains("float") || storeType.Contains("double")) return "float";

        // Date/time
        if (storeType.Contains("timestamp") || storeType.Contains("datetime") || storeType.Contains("date") || storeType.Contains("time")) return "datetime";

        // Binary
        if (storeType.Contains("bytea") || storeType.Contains("varbinary") || storeType.Contains("binary") || storeType.Contains("image")) return "binary";

        // JSON
        if (storeType.Contains("json")) return "json";

        return "unknown";
    }
}
