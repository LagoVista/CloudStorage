using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Org
{
    // generated: full source query and reader
    public static class OrgSourceReader
    {
        public const string SourceQuery = @"
SELECT
    OrgId,
    OrgName,
    OrgBillingContactId,
    Status,
    CreationDate,
    LastUpdatedDate
FROM dbo.Org
ORDER BY OrgId;";

        public static async Task<List<SourceOrgRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceOrgRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceOrgRow
                {
                    OrgId = reader.IsDBNull(reader.GetOrdinal("OrgId")) ? null : reader["OrgId"].ToString(),
                    OrgName = reader.IsDBNull(reader.GetOrdinal("OrgName")) ? null : reader["OrgName"].ToString(),
                    OrgBillingContactId = reader.IsDBNull(reader.GetOrdinal("OrgBillingContactId")) ? null : reader["OrgBillingContactId"].ToString(),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdatedDate = reader.GetDateTime(reader.GetOrdinal("LastUpdatedDate")),
                });
            }

            return rows;
        }
    }
}
