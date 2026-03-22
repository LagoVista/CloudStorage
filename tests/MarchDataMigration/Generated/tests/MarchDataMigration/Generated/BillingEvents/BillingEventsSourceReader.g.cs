using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.BillingEvents
{
    // generated: full source query and reader
    public static class BillingEventsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    ResourceId,
    ResourceName,
    SubscriptionId,
    ProductId,
    StartTimeStamp,
    StartedByAppUserId,
    EndTimeStamp,
    EndedByAppUserId,
    HoursBilled,
    UnitCost,
    DiscountPercent,
    Extended,
    UnitTypeId,
    Notes,
    Status,
    UnitPrice
FROM dbo.BillingEvents
ORDER BY Id;";

        public static async Task<List<SourceBillingEventsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceBillingEventsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceBillingEventsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    ResourceId = reader.IsDBNull(reader.GetOrdinal("ResourceId")) ? null : reader["ResourceId"].ToString(),
                    ResourceName = reader.IsDBNull(reader.GetOrdinal("ResourceName")) ? null : reader["ResourceName"].ToString(),
                    SubscriptionId = reader.GetGuid(reader.GetOrdinal("SubscriptionId")),
                    ProductId = reader.GetGuid(reader.GetOrdinal("ProductId")),
                    StartTimeStamp = reader.GetDateTime(reader.GetOrdinal("StartTimeStamp")),
                    StartedByAppUserId = reader.IsDBNull(reader.GetOrdinal("StartedByAppUserId")) ? null : reader["StartedByAppUserId"].ToString(),
                    EndTimeStamp = reader.IsDBNull(reader.GetOrdinal("EndTimeStamp")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTimeStamp")),
                    EndedByAppUserId = reader.IsDBNull(reader.GetOrdinal("EndedByAppUserId")) ? null : reader["EndedByAppUserId"].ToString(),
                    HoursBilled = reader.IsDBNull(reader.GetOrdinal("HoursBilled")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("HoursBilled")),
                    UnitCost = reader.IsDBNull(reader.GetOrdinal("UnitCost")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("UnitCost")),
                    DiscountPercent = reader.IsDBNull(reader.GetOrdinal("DiscountPercent")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("DiscountPercent")),
                    Extended = reader.IsDBNull(reader.GetOrdinal("Extended")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("Extended")),
                    UnitTypeId = reader.GetInt32(reader.GetOrdinal("UnitTypeId")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    UnitPrice = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                });
            }

            return rows;
        }
    }
}
