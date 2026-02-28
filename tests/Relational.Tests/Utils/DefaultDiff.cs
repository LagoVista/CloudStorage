using System;
using System.Collections.Generic;
using System.Linq;

public static class DefaultDiff
{
    public static List<string> Compare(TableDefaultsShape db, EfTableDefaultsShape ef)
    {
        var diffs = new List<string>();

        var dbMap = db.Defaults.ToDictionary(d => d.ColumnName, d => d.DefaultSqlNormalized, StringComparer.OrdinalIgnoreCase);
        var efMap = ef.Defaults.ToDictionary(d => d.ColumnName, d => d.DefaultSqlNormalized, StringComparer.OrdinalIgnoreCase);

        foreach (var (col, dbDef) in dbMap)
        {
            if (!efMap.TryGetValue(col, out var efDef))
            {
                diffs.Add($"Missing in EF: default on {col} = {dbDef}");
                continue;
            }

            if (!string.Equals(dbDef, efDef, StringComparison.OrdinalIgnoreCase))
                diffs.Add($"Default mismatch on {col}: DB={dbDef} EF={efDef}");
        }

        foreach (var (col, efDef) in efMap)
        {
            if (!dbMap.ContainsKey(col))
                diffs.Add($"Extra in EF: default on {col} = {efDef}");
        }

        return diffs;
    }
}