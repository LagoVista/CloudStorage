using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.PayrollSummary
{
    // generated: full source query and reader
    public static class PayrollSummarySourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    CreatedById,
    LastupdatedById,
    CreationDate,
    LastUpdateDate,
    OrganizationId,
    EncryptedTotalSalary,
    EncryptedTotalPayroll,
    EncryptedTotalExpenses,
    EncryptedTotalTaxLiability,
    EncryptedTotalRevenue,
    EncryptedTaxLiabilities,
    Status,
    Locked,
    LockedTimeStamp,
    LockedByUserId
FROM dbo.PayrollSummary
ORDER BY Id;";

        public static async Task<List<SourcePayrollSummaryRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourcePayrollSummaryRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourcePayrollSummaryRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastupdatedById = reader.IsDBNull(reader.GetOrdinal("LastupdatedById")) ? null : reader["LastupdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    EncryptedTotalSalary = reader.IsDBNull(reader.GetOrdinal("EncryptedTotalSalary")) ? null : reader["EncryptedTotalSalary"].ToString(),
                    EncryptedTotalPayroll = reader.IsDBNull(reader.GetOrdinal("EncryptedTotalPayroll")) ? null : reader["EncryptedTotalPayroll"].ToString(),
                    EncryptedTotalExpenses = reader.IsDBNull(reader.GetOrdinal("EncryptedTotalExpenses")) ? null : reader["EncryptedTotalExpenses"].ToString(),
                    EncryptedTotalTaxLiability = reader.IsDBNull(reader.GetOrdinal("EncryptedTotalTaxLiability")) ? null : reader["EncryptedTotalTaxLiability"].ToString(),
                    EncryptedTotalRevenue = reader.IsDBNull(reader.GetOrdinal("EncryptedTotalRevenue")) ? null : reader["EncryptedTotalRevenue"].ToString(),
                    EncryptedTaxLiabilities = reader.IsDBNull(reader.GetOrdinal("EncryptedTaxLiabilities")) ? null : reader["EncryptedTaxLiabilities"].ToString(),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    Locked = reader.GetBoolean(reader.GetOrdinal("Locked")),
                    LockedTimeStamp = reader.IsDBNull(reader.GetOrdinal("LockedTimeStamp")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LockedTimeStamp")),
                    LockedByUserId = reader.IsDBNull(reader.GetOrdinal("LockedByUserId")) ? null : reader["LockedByUserId"].ToString(),
                });
            }

            return rows;
        }
    }
}
