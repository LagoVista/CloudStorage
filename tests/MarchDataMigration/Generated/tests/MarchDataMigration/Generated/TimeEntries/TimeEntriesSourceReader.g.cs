using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.TimeEntries
{
    // generated: full source query and reader
    public static class TimeEntriesSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    AgreementId,
    TimePeriodId,
    BillingEventId,
    Date,
    OrganizationId,
    ProjectId,
    ProjectName,
    WorkTaskId,
    WorkTaskName,
    UserId,
    Locked,
    IsEquityTime,
    Hours,
    Notes,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate
FROM dbo.TimeEntries
ORDER BY Id;";

        public static async Task<List<SourceTimeEntriesRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceTimeEntriesRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceTimeEntriesRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    AgreementId = reader.GetGuid(reader.GetOrdinal("AgreementId")),
                    TimePeriodId = reader.GetGuid(reader.GetOrdinal("TimePeriodId")),
                    BillingEventId = reader.IsDBNull(reader.GetOrdinal("BillingEventId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("BillingEventId")),
                    Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    ProjectId = reader.IsDBNull(reader.GetOrdinal("ProjectId")) ? null : reader["ProjectId"].ToString(),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("ProjectName")) ? null : reader["ProjectName"].ToString(),
                    WorkTaskId = reader.IsDBNull(reader.GetOrdinal("WorkTaskId")) ? null : reader["WorkTaskId"].ToString(),
                    WorkTaskName = reader.IsDBNull(reader.GetOrdinal("WorkTaskName")) ? null : reader["WorkTaskName"].ToString(),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader["UserId"].ToString(),
                    Locked = reader.GetBoolean(reader.GetOrdinal("Locked")),
                    IsEquityTime = reader.GetBoolean(reader.GetOrdinal("IsEquityTime")),
                    Hours = reader.GetDecimal(reader.GetOrdinal("Hours")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                });
            }

            return rows;
        }
    }
}
