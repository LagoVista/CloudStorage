using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.PayRates
{
    // generated: full source query and reader
    public static class PayRatesSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    OrganizationId,
    UserId,
    Start,
    [End],
    IsSalary,
    FilingType,
    DeductEstimated,
    DeductEstimatedRate,
    EncryptedBillableRate,
    EncryptedInternalRate,
    EncryptedSalary,
    EncryptedDeductions,
    EncryptedEquityScaler,
    Notes,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    WorkRoleId,
    IsContractor,
    IsFTE,
    IsOfficier
FROM dbo.PayRates
ORDER BY Id;";

        public static async Task<List<SourcePayRatesRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourcePayRatesRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourcePayRatesRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader["UserId"].ToString(),
                    Start = reader.GetDateTime(reader.GetOrdinal("Start")),
                    End = reader.IsDBNull(reader.GetOrdinal("End")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("End")),
                    IsSalary = reader.GetBoolean(reader.GetOrdinal("IsSalary")),
                    FilingType = reader.IsDBNull(reader.GetOrdinal("FilingType")) ? null : reader["FilingType"].ToString(),
                    DeductEstimated = reader.GetBoolean(reader.GetOrdinal("DeductEstimated")),
                    DeductEstimatedRate = reader.GetDecimal(reader.GetOrdinal("DeductEstimatedRate")),
                    EncryptedBillableRate = reader.IsDBNull(reader.GetOrdinal("EncryptedBillableRate")) ? null : reader["EncryptedBillableRate"].ToString(),
                    EncryptedInternalRate = reader.IsDBNull(reader.GetOrdinal("EncryptedInternalRate")) ? null : reader["EncryptedInternalRate"].ToString(),
                    EncryptedSalary = reader.IsDBNull(reader.GetOrdinal("EncryptedSalary")) ? null : reader["EncryptedSalary"].ToString(),
                    EncryptedDeductions = reader.IsDBNull(reader.GetOrdinal("EncryptedDeductions")) ? null : reader["EncryptedDeductions"].ToString(),
                    EncryptedEquityScaler = reader.IsDBNull(reader.GetOrdinal("EncryptedEquityScaler")) ? null : reader["EncryptedEquityScaler"].ToString(),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    WorkRoleId = reader.IsDBNull(reader.GetOrdinal("WorkRoleId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("WorkRoleId")),
                    IsContractor = reader.GetBoolean(reader.GetOrdinal("IsContractor")),
                    IsFTE = reader.GetBoolean(reader.GetOrdinal("IsFTE")),
                    IsOfficier = reader.GetBoolean(reader.GetOrdinal("IsOfficier")),
                });
            }

            return rows;
        }
    }
}
