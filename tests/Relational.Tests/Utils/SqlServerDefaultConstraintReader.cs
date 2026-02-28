using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data;

public static class SqlServerDefaultConstraintReader
{
    public static async Task<TableDefaultsShape> ReadDefaultsAsync(SqlConnection conn, string schema, string table)
    {
        if (conn == null) throw new ArgumentNullException(nameof(conn));
        if (conn.State != ConnectionState.Open)
            throw new InvalidOperationException("SqlConnection must be open.");

        const string sql = @"
SELECT
    c.name AS ColumnName,
    dc.definition AS DefaultDefinition
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
LEFT JOIN sys.default_constraints dc
    ON dc.parent_object_id = t.object_id
   AND dc.parent_column_id = c.column_id
WHERE s.name = @schema
  AND t.name = @table
  AND dc.definition IS NOT NULL
ORDER BY c.column_id;";

        var list = new List<ColumnDefaultShape>();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var rdr = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await rdr.ReadAsync().ConfigureAwait(false))
        {
            var col = rdr.GetString(0);
            var def = rdr.GetString(1);

            list.Add(new ColumnDefaultShape(
                ColumnName: col,
                DefaultSqlNormalized: DefaultSqlNormalizer.NormalizeSqlServer(def)));
        }

        return new TableDefaultsShape(schema, table, list);
    }
}

public static class DefaultSqlNormalizer
{
    public static string NormalizeSqlServer(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return string.Empty;

        var s = definition.Trim();

        // SQL Server often wraps defaults like ((0)) or ('active') or (getdate()).
        // Strip outer parens repeatedly when they wrap the whole string.
        s = StripOuterParensRepeatedly(s);

        // Normalize whitespace
        s = string.Join(" ", s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

        // Case normalize for function-ish defaults (keep quoted strings as-is)
        // This is optional; you can remove it if you want exact-casing comparisons.
        if (!LooksQuoted(s))
            s = s.ToLowerInvariant();

        return s;
    }

    public static string NormalizeEf(string? defaultValueSql, object? defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(defaultValueSql))
        {
            var s = defaultValueSql.Trim();
            s = StripOuterParensRepeatedly(s);
            s = string.Join(" ", s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            if (!LooksQuoted(s))
                s = s.ToLowerInvariant();
            return s;
        }

        if (defaultValue == null)
            return string.Empty;

        // For EF "default value" we normalize to a SQL-ish literal form.
        // Keep it simple and predictable.
        return defaultValue switch
        {
            string str => $"'{str}'",
            bool b => b ? "1" : "0",
            _ => Convert.ToString(defaultValue, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static bool LooksQuoted(string s)
        => s.Length >= 2 && s[0] == '\'' && s[^1] == '\'';

    private static string StripOuterParensRepeatedly(string s)
    {
        while (s.Length >= 2 && s[0] == '(' && s[^1] == ')')
        {
            var inner = s.Substring(1, s.Length - 2).Trim();

            // Only strip if parentheses were truly outermost (no unbalanced parens inside).
            if (!IsBalanced(inner)) break;

            s = inner;
        }

        return s;
    }

    private static bool IsBalanced(string s)
    {
        var depth = 0;
        foreach (var ch in s)
        {
            if (ch == '(') depth++;
            if (ch == ')')
            {
                depth--;
                if (depth < 0) return false;
            }
        }
        return depth == 0;
    }
}