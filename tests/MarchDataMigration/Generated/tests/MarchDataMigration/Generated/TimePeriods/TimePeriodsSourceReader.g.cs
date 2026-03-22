using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.TimePeriods
{
    // generated: full source query and reader
    public static class TimePeriodsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    Year,
    OrganizationId,
    Locked,
    LockedByUserId,
    LockedTimeStamp,
    PayrollSummaryId,
    Start,
    [End]
FROM dbo.TimePeriods
ORDER BY Id;";

        public static async Task<List<SourceTimePeriodsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceTimePeriodsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceTimePeriodsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Year = reader.GetInt32(reader.GetOrdinal("Year")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    Locked = reader.GetBoolean(reader.GetOrdinal("Locked")),
                    LockedByUserId = reader.IsDBNull(reader.GetOrdinal("LockedByUserId")) ? null : reader["LockedByUserId"].ToString(),
                    LockedTimeStamp = reader.IsDBNull(reader.GetOrdinal("LockedTimeStamp")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LockedTimeStamp")),
                    PayrollSummaryId = reader.IsDBNull(reader.GetOrdinal("PayrollSummaryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("PayrollSummaryId")),
                    Start = reader.GetDateTime(reader.GetOrdinal("Start")),
                    End = reader.GetDateTime(reader.GetOrdinal("End")),
                });
            }

            return rows;
        }
    }
}
