using System.Data;
using System.Data.Common;

namespace SchemaVerify.Core;

public sealed class SqlServerSchemaReader : IDbSchemaReader
{
    public async Task<SchemaModel> ReadAsync(DbConnection connection, CancellationToken ct = default)
    {
        var model = new SchemaModel();

        // Tables
        var tables = new List<(string Schema, string Name)>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT s.name AS [schema], t.name AS [table]
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;";

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
  s.name AS [schema],
  t.name AS [table],
  c.name AS [column],
  ty.name AS [type_name],
  c.max_length,
  c.precision,
  c.scale,
  c.is_nullable
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;";

            using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                var schema = rdr.GetString(0);
                var tableName = rdr.GetString(1);
                var colName = rdr.GetString(2);
                var typeName = rdr.GetString(3);

                // SQL Server max_length is bytes; nvarchar uses 2 bytes/char.
                int? maxLen = rdr.IsDBNull(4) ? null : rdr.GetInt16(4);
                if (maxLen is not null && (typeName.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) || typeName.Equals("nchar", StringComparison.OrdinalIgnoreCase)))
                {
                    maxLen = maxLen / 2;
                }
                if (maxLen == -1) maxLen = null; // (max)

                int? precision = rdr.IsDBNull(5) ? null : rdr.GetByte(5);
                int? scale = rdr.IsDBNull(6) ? null : rdr.GetByte(6);
                var isNullable = !rdr.IsDBNull(7) && rdr.GetBoolean(7);

                var storeType = BuildStoreType(typeName, maxLen, precision, scale);

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
  s.name AS [schema],
  t.name AS [table],
  c.name AS [column],
  ic.key_ordinal
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.indexes i ON i.object_id = t.object_id AND i.is_primary_key = 1
JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, ic.key_ordinal;";

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

        // Stable ordering
        model.Tables.Sort((a, b) => string.Compare(a.Schema + "." + a.Name, b.Schema + "." + b.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var t in model.Tables)
        {
            t.Columns.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        return model;
    }

    private static string BuildStoreType(string typeName, int? maxLen, int? precision, int? scale)
    {
        var tn = typeName.Trim().ToLowerInvariant();

        if (tn is "nvarchar" or "varchar" or "nchar" or "char" or "varbinary" or "binary")
        {
            return maxLen is null ? $"{tn}(max)" : $"{tn}({maxLen})";
        }

        if (tn is "decimal" or "numeric")
        {
            if (precision is not null && scale is not null) return $"{tn}({precision},{scale})";
        }

        return tn;
    }
}
