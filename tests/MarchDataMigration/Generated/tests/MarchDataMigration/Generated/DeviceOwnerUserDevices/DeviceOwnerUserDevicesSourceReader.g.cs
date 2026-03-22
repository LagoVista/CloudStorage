using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.DeviceOwnerUserDevices
{
    // generated: full source query and reader
    public static class DeviceOwnerUserDevicesSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    DeviceUniqueId,
    DeviceName,
    DeviceId,
    DeviceOwnerUserId,
    ProductId,
    Discount
FROM dbo.DeviceOwnerUserDevices
ORDER BY Id;";

        public static async Task<List<SourceDeviceOwnerUserDevicesRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceDeviceOwnerUserDevicesRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceDeviceOwnerUserDevicesRow
                {
                    Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? null : reader["Id"].ToString(),
                    DeviceUniqueId = reader.IsDBNull(reader.GetOrdinal("DeviceUniqueId")) ? null : reader["DeviceUniqueId"].ToString(),
                    DeviceName = reader.IsDBNull(reader.GetOrdinal("DeviceName")) ? null : reader["DeviceName"].ToString(),
                    DeviceId = reader.IsDBNull(reader.GetOrdinal("DeviceId")) ? null : reader["DeviceId"].ToString(),
                    DeviceOwnerUserId = reader.IsDBNull(reader.GetOrdinal("DeviceOwnerUserId")) ? null : reader["DeviceOwnerUserId"].ToString(),
                    ProductId = reader.GetGuid(reader.GetOrdinal("ProductId")),
                    Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                });
            }

            return rows;
        }
    }
}
