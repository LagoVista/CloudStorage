using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


public sealed record ColumnShape(string Name, bool IsNullable, int Ordinal);

public sealed record TableShape(string Schema, string Name, IReadOnlyList<ColumnShape> Columns);
public sealed record DbTableRef(string Schema, string Table);

public static class SqlServerSchemaReader
{
    public static async Task<TableShape> ReadTableAsync(SqlConnection conn, string schema, string table)
    {
        if (conn == null) throw new ArgumentNullException(nameof(conn));
        if (string.IsNullOrWhiteSpace(schema)) throw new ArgumentNullException(nameof(schema));
        if (string.IsNullOrWhiteSpace(table)) throw new ArgumentNullException(nameof(table));

        const string sql = @"
SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
ORDER BY ORDINAL_POSITION;";

        var cols = new List<ColumnShape>();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var rdr = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await rdr.ReadAsync().ConfigureAwait(false))
        {
            var name = rdr.GetString(0);
            var isNullable = string.Equals(rdr.GetString(1), "YES", StringComparison.OrdinalIgnoreCase);
            var ordinal = rdr.GetInt32(2);

            cols.Add(new ColumnShape(name, isNullable, ordinal));
        }

        if (cols.Count == 0)
        {
            throw new InvalidOperationException($"SQL Server table not found or has no columns: {schema}.{table}");
        }

        return new TableShape(schema, table, cols);
    }
    public static async Task<IReadOnlyList<DbTableRef>> ReadAllTablesAsync(
          SqlConnection conn,
          string? schema = "dbo",
          bool includeViews = false)
    {
        if (conn == null) throw new ArgumentNullException(nameof(conn));
        if (conn.State != ConnectionState.Open)
            throw new InvalidOperationException("SqlConnection must be open.");

        // U = user table, V = view
        var types = includeViews ? new[] { "U", "V" } : new[] { "U" };
        var inList = string.Join(",", types.Select(t => $"'{t}'"));

        var sql = $@"
SELECT
    s.name  AS [SchemaName],
    o.name  AS [ObjectName]
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.type IN ({inList})
  AND (@schema IS NULL OR s.name = @schema)
  AND o.is_ms_shipped = 0
ORDER BY s.name, o.name;";

        var result = new List<DbTableRef>();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", (object?)schema ?? DBNull.Value);

        await using var rdr = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await rdr.ReadAsync().ConfigureAwait(false))
        {
            var sch = rdr.GetString(0);
            var obj = rdr.GetString(1);
            result.Add(new DbTableRef(sch, obj));
        }

        return result;
    }

    // Convenience wrapper if you want it
    public static IReadOnlyList<DbTableRef> ReadAllTables(SqlConnection conn, string? schema = "dbo", bool includeViews = false)
        => ReadAllTablesAsync(conn, schema, includeViews).GetAwaiter().GetResult();

    /// <summary>
    /// Optional helper: reads shapes for all tables by calling your existing ReadTableAsync.
    /// Keeps your schema logic in one place.
    /// </summary>
    public static async Task<IReadOnlyList<TableShape>> ReadAllTableShapesAsync(
        SqlConnection conn,
        string? schema = "dbo",
        bool includeViews = false)
    {
        var tables = await ReadAllTablesAsync(conn, schema, includeViews).ConfigureAwait(false);
        var shapes = new List<TableShape>(tables.Count);

        for (var i = 0; i < tables.Count; i++)
        {
            var t = tables[i];
            shapes.Add(await ReadTableAsync(conn, t.Schema, t.Table).ConfigureAwait(false));
        }

        return shapes;
    }

}