using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

public sealed record ColumnTypeShape(string ColumnName, string StoreType);

public static class SqlServerColumnTypeReader
{
    public static async Task<IReadOnlyList<ColumnTypeShape>> ReadColumnTypesAsync(SqlConnection conn, string schema, string table)
    {
        if (conn == null) throw new ArgumentNullException(nameof(conn));
        if (conn.State != ConnectionState.Open) throw new InvalidOperationException("SqlConnection must be open.");

        const string sql = @"
SELECT
    c.name AS ColumnName,
    ty.name AS TypeName,
    c.max_length,
    c.precision,
    c.scale
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = @schema
  AND t.name = @table
ORDER BY c.column_id;";

        var list = new List<ColumnTypeShape>();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var rdr = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await rdr.ReadAsync().ConfigureAwait(false))
        {
            var col = rdr.GetString(0);
            var typeName = rdr.GetString(1);
            var maxLen = rdr.GetInt16(2);     // sys.columns.max_length is smallint
            var precision = rdr.GetByte(3);   // tinyint
            var scale = rdr.GetByte(4);       // tinyint

            var storeType = SqlServerTypeFormatter.Format(typeName, maxLen, precision, scale);
            list.Add(new ColumnTypeShape(col, storeType));
        }

        return list;
    }
}

public static class SqlServerTypeFormatter
{
    public static string Format(string typeName, short maxLength, byte precision, byte scale)
    {
        // Normalize to lowercase, no spaces.
        var t = (typeName ?? string.Empty).Trim().ToLowerInvariant();

        return t switch
        {
            // (n)char/(n)varchar: max_length is bytes; nvarchar/nchar are 2 bytes per char
            "varchar" => maxLength == -1 ? "varchar(max)" : $"varchar({maxLength})",
            "char" => $"char({maxLength})",
            "varbinary" => maxLength == -1 ? "varbinary(max)" : $"varbinary({maxLength})",
            "binary" => $"binary({maxLength})",

            "nvarchar" => maxLength == -1 ? "nvarchar(max)" : $"nvarchar({maxLength / 2})",
            "nchar" => $"nchar({maxLength / 2})",

            // numerics
            "decimal" => $"decimal({precision},{scale})",
            "numeric" => $"numeric({precision},{scale})",

            // date/time with scale
            "datetime2" => $"datetime2({scale})",
            "datetimeoffset" => $"datetimeoffset({scale})",
            "time" => $"time({scale})",

            // fixed names
            _ => t
        };
    }
}