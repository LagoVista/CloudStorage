using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Subscription
{
    // generated: full source query and reader
    public static class SubscriptionSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdatedDate,
    OrgId,
    Name,
    [Key],
    Status,
    Description,
    CustomerId,
    PaymentToken,
    PaymentTokenDate,
    PaymentTokenExpires,
    PaymentTokenStatus,
    Icon
FROM dbo.Subscription
ORDER BY Id;";

        public static async Task<List<SourceSubscriptionRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceSubscriptionRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceSubscriptionRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdatedDate = reader.GetDateTime(reader.GetOrdinal("LastUpdatedDate")),
                    OrgId = reader.IsDBNull(reader.GetOrdinal("OrgId")) ? null : reader["OrgId"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    CustomerId = reader.IsDBNull(reader.GetOrdinal("CustomerId")) ? null : reader["CustomerId"].ToString(),
                    PaymentToken = reader.IsDBNull(reader.GetOrdinal("PaymentToken")) ? null : reader["PaymentToken"].ToString(),
                    PaymentTokenDate = reader.IsDBNull(reader.GetOrdinal("PaymentTokenDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("PaymentTokenDate")),
                    PaymentTokenExpires = reader.IsDBNull(reader.GetOrdinal("PaymentTokenExpires")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("PaymentTokenExpires")),
                    PaymentTokenStatus = reader.IsDBNull(reader.GetOrdinal("PaymentTokenStatus")) ? null : reader["PaymentTokenStatus"].ToString(),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader["Icon"].ToString(),
                });
            }

            return rows;
        }
    }
}
