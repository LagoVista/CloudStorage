using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.RecurringCycleType
{
    // generated: full source query and reader
    public static class RecurringCycleTypeSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    [Key],
    Name
FROM dbo.RecurringCycleType
ORDER BY Id;";

        public static async Task<List<SourceRecurringCycleTypeRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceRecurringCycleTypeRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceRecurringCycleTypeRow
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                });
            }

            return rows;
        }
    }
}
