using System;
using System.Collections.Generic;
using System.Linq;

public static class ColumnTypeDiff
{
    public static List<string> CompareStrict(IReadOnlyList<ColumnTypeShape> db, IReadOnlyList<ColumnTypeShape> ef)
    {
        var diffs = new List<string>();

        var dbMap = db.ToDictionary(x => x.ColumnName, x => Normalize(x.StoreType), StringComparer.OrdinalIgnoreCase);
        var efMap = ef.ToDictionary(x => x.ColumnName, x => Normalize(x.StoreType), StringComparer.OrdinalIgnoreCase);

        foreach (var (col, dbType) in dbMap)
        {
            if (!efMap.TryGetValue(col, out var efType))
            {
                diffs.Add($"Missing in EF: store type for {col} = {dbType}");
                continue;
            }

            if (!string.Equals(dbType, efType, StringComparison.OrdinalIgnoreCase))
                diffs.Add($"Type mismatch on {col}: DB={dbType} EF={efType}");
        }

        foreach (var (col, efType) in efMap)
        {
            if (!dbMap.ContainsKey(col))
                diffs.Add($"Extra in EF: store type for {col} = {efType}");
        }

        return diffs;

        static string Normalize(string s)
            => (s ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", string.Empty);
    }
}