using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

sealed class FkRow
{
    public string Name { get; init; } = default!;
    public string FromSchema { get; init; } = default!;
    public string FromTable { get; init; } = default!;
    public string FromColumn { get; init; } = default!;
    public string ToSchema { get; init; } = default!;
    public string ToTable { get; init; } = default!;
    public string ToColumn { get; init; } = default!;
    public string OnDelete { get; init; } = default!;
    public int Ordinal { get; init; }
}

public sealed record DbForeignKey(
    string Name,
    string FromSchema,
    string FromTable,
    string[] FromColumns,
    string ToSchema,
    string ToTable,
    string[] ToColumns,
    DeleteAction OnDelete);

public static class SqlServerForeignKeyReader
{
    public static async Task<IReadOnlyList<DbForeignKey>> ReadOutboundFksAsync(
        SqlConnection conn,
        string schema,
        string table)
    {
        const string sql = @"
SELECT
    fk.name,
    sch_from.name  AS FromSchema,
    tab_from.name  AS FromTable,
    col_from.name  AS FromColumn,
    sch_to.name    AS ToSchema,
    tab_to.name    AS ToTable,
    col_to.name    AS ToColumn,
    fk.delete_referential_action_desc AS OnDeleteAction,
    fkc.constraint_column_id AS Ordinal
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables tab_from ON fk.parent_object_id = tab_from.object_id
JOIN sys.schemas sch_from ON tab_from.schema_id = sch_from.schema_id
JOIN sys.columns col_from ON fkc.parent_object_id = col_from.object_id AND fkc.parent_column_id = col_from.column_id
JOIN sys.tables tab_to ON fk.referenced_object_id = tab_to.object_id
JOIN sys.schemas sch_to ON tab_to.schema_id = sch_to.schema_id
JOIN sys.columns col_to ON fkc.referenced_object_id = col_to.object_id AND fkc.referenced_column_id = col_to.column_id
WHERE sch_from.name = @schema
  AND tab_from.name = @table
ORDER BY fk.name, fkc.constraint_column_id;";

        var rows = new List<FkRow>();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            rows.Add(new FkRow
            {
                Name = rdr.GetString(0),
                FromSchema = rdr.GetString(1),
                FromTable = rdr.GetString(2),
                FromColumn = rdr.GetString(3),
                ToSchema = rdr.GetString(4),
                ToTable = rdr.GetString(5),
                ToColumn = rdr.GetString(6),
                OnDelete = rdr.GetString(7),
                Ordinal = rdr.GetInt32(8)
            });
        }

        return rows
            .GroupBy(r => r.Name)
            .Select(g =>
            {
                var first = g.First();
                return new DbForeignKey(
                    first.Name,
                    first.FromSchema,
                    first.FromTable,
                    g.OrderBy(x => x.Ordinal).Select(x => x.FromColumn).ToArray(),
                    first.ToSchema,
                    first.ToTable,
                    g.OrderBy(x => x.Ordinal).Select(x => x.ToColumn).ToArray(),
                    DeleteActionMaps.FromSqlServer(first.OnDelete));
            })
            .ToList();
    }
}