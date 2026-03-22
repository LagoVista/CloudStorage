using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.DeviceOwnerUser
{
    // generated: full source query and reader
    public static class DeviceOwnerUserSourceReader
    {
        public const string SourceQuery = @"
SELECT
    DeviceOwnerUserId,
    Email,
    Phone,
    FullName,
    CreationDate,
    LastUpdatedDate
FROM dbo.DeviceOwnerUser
ORDER BY DeviceOwnerUserId;";

        public static async Task<List<SourceDeviceOwnerUserRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceDeviceOwnerUserRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceDeviceOwnerUserRow
                {
                    DeviceOwnerUserId = reader.IsDBNull(reader.GetOrdinal("DeviceOwnerUserId")) ? null : reader["DeviceOwnerUserId"].ToString(),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader["Email"].ToString(),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader["Phone"].ToString(),
                    FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader["FullName"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdatedDate = reader.GetDateTime(reader.GetOrdinal("LastUpdatedDate")),
                });
            }

            return rows;
        }
    }
}
