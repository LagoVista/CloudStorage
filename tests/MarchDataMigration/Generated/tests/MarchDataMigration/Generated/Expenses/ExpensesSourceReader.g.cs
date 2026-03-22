using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Expenses
{
    // generated: full source query and reader
    public static class ExpensesSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    TimePeriodId,
    ExpenseCategoryId,
    AgreementId,
    BillingEventId,
    PaymentId,
    Date,
    ProjectId,
    ProjectName,
    WorkTaskId,
    WorkTaskName,
    UserId,
    OrganizationId,
    Approved,
    ApprovedById,
    ApprovedDate,
    Locked,
    EncryptedAmount,
    EncryptedReimbursedAmount,
    Notes,
    Description,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    VendorId
FROM dbo.Expenses
ORDER BY Id;";

        public static async Task<List<SourceExpensesRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceExpensesRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceExpensesRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    TimePeriodId = reader.GetGuid(reader.GetOrdinal("TimePeriodId")),
                    ExpenseCategoryId = reader.IsDBNull(reader.GetOrdinal("ExpenseCategoryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ExpenseCategoryId")),
                    AgreementId = reader.IsDBNull(reader.GetOrdinal("AgreementId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("AgreementId")),
                    BillingEventId = reader.IsDBNull(reader.GetOrdinal("BillingEventId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("BillingEventId")),
                    PaymentId = reader.IsDBNull(reader.GetOrdinal("PaymentId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("PaymentId")),
                    Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                    ProjectId = reader.IsDBNull(reader.GetOrdinal("ProjectId")) ? null : reader["ProjectId"].ToString(),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("ProjectName")) ? null : reader["ProjectName"].ToString(),
                    WorkTaskId = reader.IsDBNull(reader.GetOrdinal("WorkTaskId")) ? null : reader["WorkTaskId"].ToString(),
                    WorkTaskName = reader.IsDBNull(reader.GetOrdinal("WorkTaskName")) ? null : reader["WorkTaskName"].ToString(),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader["UserId"].ToString(),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    Approved = reader.GetBoolean(reader.GetOrdinal("Approved")),
                    ApprovedById = reader.IsDBNull(reader.GetOrdinal("ApprovedById")) ? null : reader["ApprovedById"].ToString(),
                    ApprovedDate = reader.IsDBNull(reader.GetOrdinal("ApprovedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ApprovedDate")),
                    Locked = reader.GetBoolean(reader.GetOrdinal("Locked")),
                    EncryptedAmount = reader.IsDBNull(reader.GetOrdinal("EncryptedAmount")) ? null : reader["EncryptedAmount"].ToString(),
                    EncryptedReimbursedAmount = reader.IsDBNull(reader.GetOrdinal("EncryptedReimbursedAmount")) ? null : reader["EncryptedReimbursedAmount"].ToString(),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
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
