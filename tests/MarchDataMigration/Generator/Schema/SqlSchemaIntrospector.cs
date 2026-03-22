using MarchDataMigration.Generator.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generator.Schema
{
    public sealed class SqlSchemaIntrospector
    {
        private const string TableSql = @"
SELECT
    s.name AS SchemaName,
    t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;";

        private const string ColumnSql = @"
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS SqlTypeName,
    c.is_nullable AS IsNullable,
    c.column_id AS Ordinal,
    CASE WHEN pk.column_id IS NULL THEN 0 ELSE 1 END AS IsPrimaryKey
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN
(
    SELECT ic.object_id, ic.column_id
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.is_primary_key = 1
) pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;";

        public async Task<IReadOnlyList<SchemaTableInfo>> LoadAsync(string connectionString, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var tables = new Dictionary<string, SchemaTableInfo>(StringComparer.OrdinalIgnoreCase);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using (var tableCommand = new SqlCommand(TableSql, connection))
            await using (var tableReader = await tableCommand.ExecuteReaderAsync(ct))
            {
                while (await tableReader.ReadAsync(ct))
                {
                    var schemaName = tableReader["SchemaName"].ToString();
                    var tableName = tableReader["TableName"].ToString();
                    tables[$"{schemaName}.{tableName}"] = new SchemaTableInfo
                    {
                        SchemaName = schemaName,
                        TableName = tableName
                    };
                }
            }

            await using (var columnCommand = new SqlCommand(ColumnSql, connection))
            await using (var columnReader = await columnCommand.ExecuteReaderAsync(ct))
            {
                while (await columnReader.ReadAsync(ct))
                {
                    var schemaName = columnReader["SchemaName"].ToString();
                    var tableName = columnReader["TableName"].ToString();
                    var key = $"{schemaName}.{tableName}";

                    if (!tables.TryGetValue(key, out var table))
                        continue;

                    table.Columns.Add(new SchemaColumnInfo
                    {
                        Name = columnReader["ColumnName"].ToString(),
                        SqlTypeName = columnReader["SqlTypeName"].ToString(),
                        IsNullable = Convert.ToBoolean(columnReader["IsNullable"]),
                        Ordinal = Convert.ToInt32(columnReader["Ordinal"]),
                        IsPrimaryKey = Convert.ToBoolean(columnReader["IsPrimaryKey"])
                    });
                }
            }

            return tables.Values.OrderBy(t => t.SchemaName).ThenBy(t => t.TableName).ToList();
        }
    }
}
