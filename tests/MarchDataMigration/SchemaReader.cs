using System;
using System.Collections.Generic;
using System.Text;

namespace MarchDataMigration
{
    using global::LegacyMigrationScaffolding.Schema;
    // ================================
    // File: LegacyMigrationScaffolding/Schema/SchemaReader.cs
    // ================================
    using Microsoft.Data.SqlClient;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    namespace LegacyMigrationScaffolding.Schema
    {
        public sealed class SchemaReader
        {
            public async Task<List<SchemaTableInfo>> ReadTablesAsync(string connectionString, SchemaReadOptions options, CancellationToken cancellationToken = default)
            {
                var tables = new Dictionary<string, SchemaTableInfo>();

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken);

                var sql = @"
SELECT
    c.TABLE_SCHEMA,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    CASE WHEN c.IS_NULLABLE = 'YES' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IS_NULLABLE,
    c.ORDINAL_POSITION,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IS_PRIMARY_KEY,
    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IS_IDENTITY
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN
(
    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
        AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
        AND tc.TABLE_NAME = ku.TABLE_NAME
    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
    AND c.TABLE_NAME = pk.TABLE_NAME
    AND c.COLUMN_NAME = pk.COLUMN_NAME
WHERE c.TABLE_SCHEMA = @schemaName
ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@schemaName", SqlDbType.NVarChar) { Value = options.SchemaName });

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var schemaName = reader.GetString(reader.GetOrdinal("TABLE_SCHEMA"));
                    var tableName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
                    var key = $"{schemaName}.{tableName}";

                    if (!tables.TryGetValue(key, out var table))
                    {
                        table = new SchemaTableInfo
                        {
                            SchemaName = schemaName,
                            TableName = tableName
                        };

                        tables.Add(key, table);
                    }

                    table.Columns.Add(new SchemaColumnInfo
                    {
                        ColumnName = reader.GetString(reader.GetOrdinal("COLUMN_NAME")),
                        SqlDataType = reader.GetString(reader.GetOrdinal("DATA_TYPE")),
                        IsNullable = reader.GetBoolean(reader.GetOrdinal("IS_NULLABLE")),
                        OrdinalPosition = reader.GetInt32(reader.GetOrdinal("ORDINAL_POSITION")),
                        MaxLength = reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH")) ? (int?)null : System.Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"]),
                        Precision = reader.IsDBNull(reader.GetOrdinal("NUMERIC_PRECISION")) ? (int?)null : System.Convert.ToInt32(reader["NUMERIC_PRECISION"]),
                        Scale = reader.IsDBNull(reader.GetOrdinal("NUMERIC_SCALE")) ? (int?)null : System.Convert.ToInt32(reader["NUMERIC_SCALE"]),
                        IsPrimaryKey = reader.GetBoolean(reader.GetOrdinal("IS_PRIMARY_KEY")),
                        IsIdentity = !reader.IsDBNull(reader.GetOrdinal("IS_IDENTITY")) && System.Convert.ToInt32(reader["IS_IDENTITY"]) == 1
                    });
                }

                return tables.Values.OrderBy(x => x.SchemaName).ThenBy(x => x.TableName).ToList();
            }
        }
    }
}
