namespace SchemaVerify.Core;

public sealed class FamilyTypeMatcher : ITypeMatcher
{
    private readonly string _provider;

    public FamilyTypeMatcher(string provider)
    {
        _provider = (provider ?? string.Empty).Trim().ToLowerInvariant();
    }

    public bool AreEquivalent(ColumnModel ef, ColumnModel db, out string? reason)
    {
        var efNorm = TypeNormalizer.Normalize(_provider, ef);
        var dbNorm = TypeNormalizer.Normalize(_provider, db);

        if (!string.Equals(efNorm.Family, dbNorm.Family, StringComparison.OrdinalIgnoreCase))
        {
            reason = $"Type family mismatch: EF='{efNorm.Family}' ({ef.StoreType}) DB='{dbNorm.Family}' ({db.StoreType})";
            return false;
        }

        // Facets: only compare when meaningful for that family.
        if (efNorm.CompareMaxLength && efNorm.MaxLength != dbNorm.MaxLength)
        {
            reason = $"MaxLength mismatch: EF={efNorm.MaxLength?.ToString() ?? "(null)"} DB={dbNorm.MaxLength?.ToString() ?? "(null)"}";
            return false;
        }

        if (efNorm.ComparePrecisionScale && (efNorm.Precision != dbNorm.Precision || efNorm.Scale != dbNorm.Scale))
        {
            reason = $"Precision/Scale mismatch: EF={Fmt(efNorm)} DB={Fmt(dbNorm)}";
            return false;
        }

        reason = null;
        return true;
    }

    private static string Fmt(NormalizedType t)
        => $"{t.Precision?.ToString() ?? "(null)"},{t.Scale?.ToString() ?? "(null)"}";
}
