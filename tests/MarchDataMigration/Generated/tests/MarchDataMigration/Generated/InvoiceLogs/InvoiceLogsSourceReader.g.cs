using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.InvoiceLogs
{
    // generated: full source query and reader
    public static class InvoiceLogsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    InvoiceId,
    DateStamp,
    EventId,
    EventData,
    Message
FROM dbo.InvoiceLogs
ORDER BY Id;";

        public static async Task<List<SourceInvoiceLogsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceInvoiceLogsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceInvoiceLogsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    InvoiceId = reader.GetGuid(reader.GetOrdinal("InvoiceId")),
                    DateStamp = reader.GetDateTime(reader.GetOrdinal("DateStamp")),
                    EventId = reader.IsDBNull(reader.GetOrdinal("EventId")) ? null : reader["EventId"].ToString(),
                    EventData = reader.IsDBNull(reader.GetOrdinal("EventData")) ? null : reader["EventData"].ToString(),
                    Message = reader.IsDBNull(reader.GetOrdinal("Message")) ? null : reader["Message"].ToString(),
                });
            }

            return rows;
        }
    }
}
