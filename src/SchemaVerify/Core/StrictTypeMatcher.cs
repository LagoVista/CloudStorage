namespace SchemaVerify.Core;

public sealed class StrictTypeMatcher : ITypeMatcher
{
    public bool AreEquivalent(ColumnModel ef, ColumnModel db, out string? reason)
    {
        var efType = Normalize(ef.StoreType);
        var dbType = Normalize(db.StoreType);

        if (!string.Equals(efType, dbType, StringComparison.OrdinalIgnoreCase))
        {
            reason = $"StoreType mismatch: EF='{ef.StoreType}' DB='{db.StoreType}'";
            return false;
        }

        // Even in strict mode, keep these checks separate so the diff is more readable.
        if (ef.MaxLength != db.MaxLength)
        {
            reason = $"MaxLength mismatch: EF={ef.MaxLength?.ToString() ?? "(null)"} DB={db.MaxLength?.ToString() ?? "(null)"}";
            return false;
        }

        if (ef.Precision != db.Precision || ef.Scale != db.Scale)
        {
            reason = $"Precision/Scale mismatch: EF={Fmt(ef)} DB={Fmt(db)}";
            return false;
        }

        reason = null;
        return true;
    }

    private static string Normalize(string storeType)
    {
        if (string.IsNullOrWhiteSpace(storeType)) return string.Empty;

        // Lowercase, trim, collapse whitespace, normalize spaces around punctuation.
        var s = storeType.Trim().ToLowerInvariant();
        s = string.Join(' ', s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        s = s.Replace(" (", "(").Replace("( ", "(").Replace(" )", ")").Replace(", ", ",");
        return s;
    }

    private static string Fmt(ColumnModel c)
        => $"{c.Precision?.ToString() ?? "(null)"},{c.Scale?.ToString() ?? "(null)"}";
}
