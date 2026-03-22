using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Account
{
    // generated: full source query and reader
    public static class AccountSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    Name,
    RoutingNumber,
    AccountNumber,
    Institution,
    IsLiability,
    EncryptedBalance,
    Description,
    OrganizationId,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    IsActive,
    TransactionJournalOnly
FROM dbo.Account
ORDER BY Id;";

        public static async Task<List<SourceAccountRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceAccountRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceAccountRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    RoutingNumber = reader.IsDBNull(reader.GetOrdinal("RoutingNumber")) ? null : reader["RoutingNumber"].ToString(),
                    AccountNumber = reader.IsDBNull(reader.GetOrdinal("AccountNumber")) ? null : reader["AccountNumber"].ToString(),
                    Institution = reader.IsDBNull(reader.GetOrdinal("Institution")) ? null : reader["Institution"].ToString(),
                    IsLiability = reader.GetBoolean(reader.GetOrdinal("IsLiability")),
                    EncryptedBalance = reader.IsDBNull(reader.GetOrdinal("EncryptedBalance")) ? null : reader["EncryptedBalance"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    TransactionJournalOnly = reader.GetBoolean(reader.GetOrdinal("TransactionJournalOnly")),
                });
            }

            return rows;
        }
    }
}
