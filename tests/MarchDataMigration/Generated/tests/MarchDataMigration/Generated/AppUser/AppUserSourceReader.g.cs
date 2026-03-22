using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.AppUser
{
    // generated: full source query and reader
    public static class AppUserSourceReader
    {
        public const string SourceQuery = @"
SELECT
    AppUserId,
    Email,
    FullName,
    CreationDate,
    LastUpdatedDate
FROM dbo.AppUser
ORDER BY AppUserId;";

        public static async Task<List<SourceAppUserRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceAppUserRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceAppUserRow
                {
                    AppUserId = reader.IsDBNull(reader.GetOrdinal("AppUserId")) ? null : reader["AppUserId"].ToString(),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader["Email"].ToString(),
                    FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader["FullName"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdatedDate = reader.GetDateTime(reader.GetOrdinal("LastUpdatedDate")),
                });
            }

            return rows;
        }
    }
}
