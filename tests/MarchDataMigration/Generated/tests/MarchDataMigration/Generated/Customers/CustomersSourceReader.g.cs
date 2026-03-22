using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Customers
{
    // generated: full source query and reader
    public static class CustomersSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    OrganizationId,
    CustomerName,
    BillingContactName,
    BillingContactEmail,
    Address1,
    Address2,
    City,
    State,
    Zip,
    Notes,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate
FROM dbo.Customers
ORDER BY Id;";

        public static async Task<List<SourceCustomersRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceCustomersRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceCustomersRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader["CustomerName"].ToString(),
                    BillingContactName = reader.IsDBNull(reader.GetOrdinal("BillingContactName")) ? null : reader["BillingContactName"].ToString(),
                    BillingContactEmail = reader.IsDBNull(reader.GetOrdinal("BillingContactEmail")) ? null : reader["BillingContactEmail"].ToString(),
                    Address1 = reader.IsDBNull(reader.GetOrdinal("Address1")) ? null : reader["Address1"].ToString(),
                    Address2 = reader.IsDBNull(reader.GetOrdinal("Address2")) ? null : reader["Address2"].ToString(),
                    City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader["City"].ToString(),
                    State = reader.IsDBNull(reader.GetOrdinal("State")) ? null : reader["State"].ToString(),
                    Zip = reader.IsDBNull(reader.GetOrdinal("Zip")) ? null : reader["Zip"].ToString(),
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
