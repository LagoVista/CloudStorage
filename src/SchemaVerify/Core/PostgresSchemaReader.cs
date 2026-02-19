using System.Data.Common;

namespace SchemaVerify.Core;

public sealed class PostgresSchemaReader : IDbSchemaReader
{
    public async Task<SchemaModel> ReadAsync(DbConnection connection, CancellationToken ct = default)
    {
        var model = new SchemaModel();

        // Tables (exclude system schemas)
        var tables = new List<(string Schema, string Name)>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name;";

            using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                tables.Add((rdr.GetString(0), rdr.GetString(1)));
            }
        }

        foreach (var t in tables)
        {
            model.Tables.Add(new TableModel { Schema = t.Schema, Name = t.Name });
        }

        // Columns
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT
  table_schema,
  table_name,
  column_name,
  data_type,
  udt_name,
  character_maximum_length,
  numeric_precision,
  numeric_scale,
  is_nullable
FROM information_schema.columns
WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name, ordinal_position;";

            using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                var schema = rdr.GetString(0);
                var tableName = rdr.GetString(1);
                var colName = rdr.GetString(2);
                var dataType = rdr.GetString(3);
                var udtName = rdr.GetString(4);

                int? maxLen = rdr.IsDBNull(5) ? null : rdr.GetInt32(5);
                int? precision = rdr.IsDBNull(6) ? null : rdr.GetInt32(6);
                int? scale = rdr.IsDBNull(7) ? null : rdr.GetInt32(7);

                var isNullable = string.Equals(rdr.GetString(8), "YES", StringComparison.OrdinalIgnoreCase);

                var storeType = BuildStoreType(dataType, udtName, maxLen, precision, scale);

                var table = model.FindTable(schema, tableName);
                if (table is null) continue;

                table.Columns.Add(new ColumnModel
                {
                    Name = colName,
                    StoreType = storeType,
                    IsNullable = isNullable,
                    MaxLength = maxLen,
                    Precision = precision,
                    Scale = scale
                });
            }
        }

        // Primary keys
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT
  n.nspname AS table_schema,
  c.relname AS table_name,
  a.attname AS column_name,
  k.n AS key_ordinal
FROM pg_catalog.pg_constraint con
JOIN pg_catalog.pg_class c ON c.oid = con.conrelid
JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
JOIN LATERAL unnest(con.conkey) WITH ORDINALITY AS k(attnum, n) ON true
JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid AND a.attnum = k.attnum
WHERE con.contype = 'p'
  AND n.nspname NOT IN ('pg_catalog', 'information_schema')
ORDER BY n.nspname, c.relname, k.n;";

            using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                var schema = rdr.GetString(0);
                var tableName = rdr.GetString(1);
                var colName = rdr.GetString(2);

                var table = model.FindTable(schema, tableName);
                if (table is null) continue;

                table.PrimaryKey.Add(colName);
            }
        }

        model.Tables.Sort((a, b) => string.Compare(a.Schema + "." + a.Name, b.Schema + "." + b.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var t in model.Tables)
        {
            t.Columns.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        return model;
    }

    private static string BuildStoreType(string dataType, string udtName, int? maxLen, int? precision, int? scale)
    {
        var dt = dataType.Trim().ToLowerInvariant();
        var udt = (udtName ?? string.Empty).Trim().ToLowerInvariant();

        // Prefer udt_name for some types (e.g., uuid)
        if (udt == "uuid") return "uuid";

        if (dt is "character varying")
        {
            return maxLen is null ? "character varying" : $"character varying({maxLen})";
        }

        if (dt is "character")
        {
            return maxLen is null ? "character" : $"character({maxLen})";
        }

        if (dt is "numeric" or "decimal")
        {
            if (precision is not null && scale is not null) return $"numeric({precision},{scale})";
            return "numeric";
        }

        // Common dt values already include meaning (text, integer, boolean, timestamp without time zone, etc.)
        return dt;
    }
}
