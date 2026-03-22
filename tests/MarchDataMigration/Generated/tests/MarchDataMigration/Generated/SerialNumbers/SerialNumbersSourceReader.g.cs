using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.SerialNumbers
{
    // generated: full source query and reader
    public static class SerialNumbersSourceReader
    {
        public const string SourceQuery = @"
SELECT
    [Index],
    OrgId,
    [Key],
    KeyId
FROM dbo.SerialNumbers
ORDER BY [Index];";

        public static async Task<List<SourceSerialNumbersRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceSerialNumbersRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceSerialNumbersRow
                {
                    Index = reader.GetInt32(reader.GetOrdinal("Index")),
                    OrgId = reader.IsDBNull(reader.GetOrdinal("OrgId")) ? null : reader["OrgId"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    KeyId = reader.IsDBNull(reader.GetOrdinal("KeyId")) ? null : reader["KeyId"].ToString(),
                });
            }

            return rows;
        }
    }
}
