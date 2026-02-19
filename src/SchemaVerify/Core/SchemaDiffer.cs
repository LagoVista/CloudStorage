namespace SchemaVerify.Core;

public sealed class SchemaDiffer : ISchemaDiffer
{
    public DiffReport Diff(
        string contextType,
        string provider,
        SchemaModel ef,
        SchemaModel db,
        ITypeMatcher typeMatcher)
    {
        var report = new DiffReport
        {
            ContextType = contextType,
            Provider = provider,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        // Tables expected by EF
        foreach (var efTable in ef.Tables)
        {
            var dbTable = db.FindTable(efTable.Schema, efTable.Name);
            if (dbTable is null)
            {
                report.Items.Add(new DiffItem
                {
                    Severity = DiffSeverity.Error,
                    Code = "TABLE_MISSING",
                    Message = $"Missing table in DB: {efTable.Schema}.{efTable.Name}",
                    ContextType = contextType,
                    Provider = provider,
                    Table = $"{efTable.Schema}.{efTable.Name}"
                });
                continue;
            }

            // Columns expected by EF
            foreach (var efCol in efTable.Columns)
            {
                var dbCol = dbTable.FindColumn(efCol.Name);
                if (dbCol is null)
                {
                    report.Items.Add(new DiffItem
                    {
                        Severity = DiffSeverity.Error,
                        Code = "COLUMN_MISSING",
                        Message = $"Missing column in DB: {efTable.Schema}.{efTable.Name}.{efCol.Name}",
                        ContextType = contextType,
                        Provider = provider,
                        Table = $"{efTable.Schema}.{efTable.Name}",
                        Column = efCol.Name
                    });
                    continue;
                }

                // Nullability
                if (efCol.IsNullable != dbCol.IsNullable)
                {
                    report.Items.Add(new DiffItem
                    {
                        Severity = DiffSeverity.Error,
                        Code = "NULLABILITY_MISMATCH",
                        Message = $"Nullability mismatch on {efTable.Schema}.{efTable.Name}.{efCol.Name}: EF={(efCol.IsNullable ? "NULL" : "NOT NULL")} DB={(dbCol.IsNullable ? "NULL" : "NOT NULL")}",
                        ContextType = contextType,
                        Provider = provider,
                        Table = $"{efTable.Schema}.{efTable.Name}",
                        Column = efCol.Name
                    });
                }

                // Type
                if (!typeMatcher.AreEquivalent(efCol, dbCol, out var reason))
                {
                    report.Items.Add(new DiffItem
                    {
                        Severity = DiffSeverity.Error,
                        Code = "TYPE_MISMATCH",
                        Message = $"Type mismatch on {efTable.Schema}.{efTable.Name}.{efCol.Name}: {reason}",
                        ContextType = contextType,
                        Provider = provider,
                        Table = $"{efTable.Schema}.{efTable.Name}",
                        Column = efCol.Name
                    });
                }
            }

            // Primary key
            if (efTable.PrimaryKey.Count > 0)
            {
                if (dbTable.PrimaryKey.Count == 0)
                {
                    report.Items.Add(new DiffItem
                    {
                        Severity = DiffSeverity.Error,
                        Code = "PK_MISSING",
                        Message = $"Missing primary key in DB: {efTable.Schema}.{efTable.Name} (expected: {string.Join(",", efTable.PrimaryKey)})",
                        ContextType = contextType,
                        Provider = provider,
                        Table = $"{efTable.Schema}.{efTable.Name}"
                    });
                }
                else if (!SequenceEqualCI(efTable.PrimaryKey, dbTable.PrimaryKey))
                {
                    report.Items.Add(new DiffItem
                    {
                        Severity = DiffSeverity.Error,
                        Code = "PK_MISMATCH",
                        Message = $"Primary key mismatch on {efTable.Schema}.{efTable.Name}: EF=({string.Join(",", efTable.PrimaryKey)}) DB=({string.Join(",", dbTable.PrimaryKey)})",
                        ContextType = contextType,
                        Provider = provider,
                        Table = $"{efTable.Schema}.{efTable.Name}"
                    });
                }
            }
        }

        // Extra DB tables not modeled by EF (warning)
        foreach (var dbTable in db.Tables)
        {
            var efTable = ef.FindTable(dbTable.Schema, dbTable.Name);
            if (efTable is null)
            {
                report.Items.Add(new DiffItem
                {
                    Severity = DiffSeverity.Warning,
                    Code = "TABLE_EXTRA",
                    Message = $"Extra table in DB (not in EF model): {dbTable.Schema}.{dbTable.Name}",
                    ContextType = contextType,
                    Provider = provider,
                    Table = $"{dbTable.Schema}.{dbTable.Name}"
                });
            }
        }

        return report;
    }

    private static bool SequenceEqualCI(List<string> a, List<string> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }
}
