using System;
using System.Collections.Generic;
using System.Linq;

public static class SchemaDiff
{
    public static List<string> CompareColumnsStrictOrder(TableShape truth, TableShape ef)
    {
        var diffs = new List<string>();

        var truthByName = truth.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var efByName = ef.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // Missing / extra
        foreach (var col in truthByName.Keys.OrderBy(x => x))
            if (!efByName.ContainsKey(col))
                diffs.Add($"Missing in EF model: column {col} (DB ordinal {truthByName[col].Ordinal})");

        foreach (var col in efByName.Keys.OrderBy(x => x))
            if (!truthByName.ContainsKey(col))
                diffs.Add($"Extra in EF model: column {col}");

        // Order + nullability (only for intersect)
        foreach (var name in truthByName.Keys.Intersect(efByName.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var db = truthByName[name];
            var m = efByName[name];

            // Strict: EF must specify order and match DB
            if (m.Ordinal < 0)
                diffs.Add($"Column order missing in EF: {name} should be {db.Ordinal} (add HasColumnOrder({db.Ordinal}))");
            else if (m.Ordinal != db.Ordinal)
                diffs.Add($"Column order mismatch: {name} DB={db.Ordinal} EF={m.Ordinal}");

            if (m.IsNullable != db.IsNullable)
                diffs.Add($"Nullability mismatch: {name} DB={(db.IsNullable ? "NULL" : "NOT NULL")} EF={(m.IsNullable ? "NULL" : "NOT NULL")}");
        }

        return diffs;
    }
}