using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ExpenseCategory
{
    // generated: full source query and reader
    public static class ExpenseCategorySourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    CreatedById,
    CreationDate,
    LastUpdatedById,
    LastUpdateDate,
    OrganizationId,
    [Key],
    Name,
    Description,
    ReimbursementPercent,
    DeductiblePercent,
    IsActive,
    RequiresApproval,
    Icon,
    TaxCategory
FROM dbo.ExpenseCategory
ORDER BY Id;";

        public static async Task<List<SourceExpenseCategoryRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceExpenseCategoryRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceExpenseCategoryRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    ReimbursementPercent = reader.GetDecimal(reader.GetOrdinal("ReimbursementPercent")),
                    DeductiblePercent = reader.GetDecimal(reader.GetOrdinal("DeductiblePercent")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    RequiresApproval = reader.GetBoolean(reader.GetOrdinal("RequiresApproval")),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader["Icon"].ToString(),
                    TaxCategory = reader.IsDBNull(reader.GetOrdinal("TaxCategory")) ? null : reader["TaxCategory"].ToString(),
                });
            }

            return rows;
        }
    }
}
