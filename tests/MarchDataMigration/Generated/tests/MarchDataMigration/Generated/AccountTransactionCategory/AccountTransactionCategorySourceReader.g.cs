using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.AccountTransactionCategory
{
    // generated: full source query and reader
    public static class AccountTransactionCategorySourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    OrganizationId,
    Name,
    Type,
    Description,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    IsActive,
    Icon,
    ExpenseCategoryId,
    TaxCategory,
    TaxReportable,
    Passthrough
FROM dbo.AccountTransactionCategory
ORDER BY Id;";

        public static async Task<List<SourceAccountTransactionCategoryRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceAccountTransactionCategoryRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceAccountTransactionCategoryRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? null : reader["Type"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader["Icon"].ToString(),
                    ExpenseCategoryId = reader.IsDBNull(reader.GetOrdinal("ExpenseCategoryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ExpenseCategoryId")),
                    TaxCategory = reader.IsDBNull(reader.GetOrdinal("TaxCategory")) ? null : reader["TaxCategory"].ToString(),
                    TaxReportable = reader.GetBoolean(reader.GetOrdinal("TaxReportable")),
                    Passthrough = reader.GetBoolean(reader.GetOrdinal("Passthrough")),
                });
            }

            return rows;
        }
    }
}
