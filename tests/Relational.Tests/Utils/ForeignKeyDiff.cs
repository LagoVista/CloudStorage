using System;
using System.Collections.Generic;
using System.Linq;

public static class ForeignKeyDiff
{
    public static List<string> Compare(
        IReadOnlyList<DbForeignKey> dbFks,
        IReadOnlyList<EfFkShape> efFks)
    {
        var diffs = new List<string>();

        foreach (var db in dbFks)
        {
            var match = efFks.FirstOrDefault(ef =>
                db.FromColumns.SequenceEqual(ef.FromColumns, StringComparer.OrdinalIgnoreCase) &&
                db.ToSchema.Equals(ef.ToSchema, StringComparison.OrdinalIgnoreCase) &&
                db.ToTable.Equals(ef.ToTable, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                diffs.Add($"Missing in EF: FK {db.Name} ({string.Join(",", db.FromColumns)} → {db.ToTable})");
        }

        foreach (var ef in efFks)
        {
            var match = dbFks.FirstOrDefault(db =>
                db.FromColumns.SequenceEqual(ef.FromColumns, StringComparer.OrdinalIgnoreCase) &&
                db.ToSchema.Equals(ef.ToSchema, StringComparison.OrdinalIgnoreCase) &&
                db.ToTable.Equals(ef.ToTable, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                diffs.Add($"Extra in EF: FK ({string.Join(",", ef.FromColumns)} → {ef.ToTable})");
        }

        return diffs;
    }
}