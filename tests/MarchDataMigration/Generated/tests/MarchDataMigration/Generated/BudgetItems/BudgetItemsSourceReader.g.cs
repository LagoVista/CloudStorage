using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.BudgetItems
{
    // generated: full source query and reader
    public static class BudgetItemsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    Name,
    Icon,
    Year,
    Month,
    OrganizationId,
    AccountTransactionCategoryId,
    ExpenseCategoryId,
    WorkRoleId,
    EncryptedAllocated,
    EncryptedActual,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    Description
FROM dbo.BudgetItems
ORDER BY Id;";

        public static async Task<List<SourceBudgetItemsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceBudgetItemsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceBudgetItemsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader["Icon"].ToString(),
                    Year = reader.GetInt32(reader.GetOrdinal("Year")),
                    Month = reader.GetInt32(reader.GetOrdinal("Month")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    AccountTransactionCategoryId = reader.IsDBNull(reader.GetOrdinal("AccountTransactionCategoryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("AccountTransactionCategoryId")),
                    ExpenseCategoryId = reader.IsDBNull(reader.GetOrdinal("ExpenseCategoryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ExpenseCategoryId")),
                    WorkRoleId = reader.IsDBNull(reader.GetOrdinal("WorkRoleId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("WorkRoleId")),
                    EncryptedAllocated = reader.IsDBNull(reader.GetOrdinal("EncryptedAllocated")) ? null : reader["EncryptedAllocated"].ToString(),
                    EncryptedActual = reader.IsDBNull(reader.GetOrdinal("EncryptedActual")) ? null : reader["EncryptedActual"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                });
            }

            return rows;
        }
    }
}
