using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

public class SchemaDiffColumn
{
    public SchemaDiffColumn(string columnName, string message, string dbSuggestion = "")
    {
        Message = message;
        ColumnName = columnName;
        DbSuggestion = dbSuggestion;
    }

    public string ColumnName { get; set; }  
    public string DbSuggestion { get; set; }

    public string Message { get; set; }
    public override string ToString() => Message;
}

public static class SchemaDiff
{
    public static List<SchemaDiffColumn> CompareColumnsStrictOrder(IReadOnlyList<ColumnTypeShape> truth, TableShape ef, string schema, string tableName)
    {
        var diffs = new List<SchemaDiffColumn>();

        var truthByName = truth.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
        var efByName = ef.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // Missing / extra
        foreach (var col in truthByName.Keys.OrderBy(x => x))
            if (!efByName.ContainsKey(col))
                diffs.Add(new SchemaDiffColumn(col, $"Missing in EF model: column {col} (DB ordinal {truthByName[col].Ordinal})"));

        foreach (var col in efByName.Keys.OrderBy(x => x))
            if (!truthByName.ContainsKey(col))
                diffs.Add(new SchemaDiffColumn(col, $"Extra in EF model: column {col}"));

        // Order + nullability (only for intersect)
        foreach (var name in truthByName.Keys.Intersect(efByName.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var db = truthByName[name];
            var m = efByName[name];

            // Strict: EF must specify order and match DB
            if (m.Ordinal < 0)
                diffs.Add(new SchemaDiffColumn(name, $"Column order missing in EF: {name} should be {db.Ordinal} (add HasColumnOrder({db.Ordinal}))"));
            else if (m.Ordinal != db.Ordinal)
                diffs.Add(new SchemaDiffColumn(name, $"Column order mismatch: {name} DB={db.Ordinal} EF={m.Ordinal}"));

            if (m.IsNullable != db.IsNullable)
            {
                var type = truth.First(c => c.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).StoreType;
                var nnClause = m.IsNullable ? "NULL" : "NOT NULL"; 
                var dbuSuggestion = $"ALTER TABLE {schema}.{tableName} ALTER COLUMN  {name} {type} {nnClause}";
                diffs.Add(new SchemaDiffColumn(name, $"Nullability mismatch: {name} DB={(db.IsNullable ? "NULL" : "NOT NULL")} EF={(m.IsNullable ? "NULL" : "NOT NULL")}", dbuSuggestion));
            }
        }

        return diffs;
    }
}