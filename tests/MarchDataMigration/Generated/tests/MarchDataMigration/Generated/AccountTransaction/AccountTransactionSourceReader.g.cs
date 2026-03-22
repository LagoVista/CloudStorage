using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.AccountTransaction
{
    // generated: full source query and reader
    public static class AccountTransactionSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    AccountId,
    TransactionDate,
    EncryptedAmount,
    IsReconciled,
    TransactionCategoryId,
    Name,
    Description,
    Tag,
    OriginalHash,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    VendorId
FROM dbo.AccountTransaction
ORDER BY Id;";

        public static async Task<List<SourceAccountTransactionRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceAccountTransactionRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceAccountTransactionRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    AccountId = reader.GetGuid(reader.GetOrdinal("AccountId")),
                    TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                    EncryptedAmount = reader.IsDBNull(reader.GetOrdinal("EncryptedAmount")) ? null : reader["EncryptedAmount"].ToString(),
                    IsReconciled = reader.GetBoolean(reader.GetOrdinal("IsReconciled")),
                    TransactionCategoryId = reader.GetGuid(reader.GetOrdinal("TransactionCategoryId")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    Tag = reader.IsDBNull(reader.GetOrdinal("Tag")) ? null : reader["Tag"].ToString(),
                    OriginalHash = reader.IsDBNull(reader.GetOrdinal("OriginalHash")) ? null : reader["OriginalHash"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    VendorId = reader.IsDBNull(reader.GetOrdinal("VendorId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("VendorId")),
                });
            }

            return rows;
        }
    }
}
