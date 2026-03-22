using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Payments
{
    // generated: full source query and reader
    public static class PaymentsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    UserId,
    TimePeriodId,
    PeriodStart,
    PeriodEnd,
    Status,
    OrganizationId,
    SubmittedDate,
    ExpectedDeliveryDate,
    BillableHours,
    InternalHours,
    EquityHours,
    Gross,
    Net,
    Expenses,
    PrimaryTransactionId,
    SecondaryTransactionId,
    PrimaryDeposit,
    EstimatedDeposit,
    ExpenseDetail,
    DeductionsDetail,
    EarnedEquity,
    ContractorPayment,
    W2Payment,
    OfficierPayment
FROM dbo.Payments
ORDER BY Id;";

        public static async Task<List<SourcePaymentsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourcePaymentsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourcePaymentsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader["UserId"].ToString(),
                    TimePeriodId = reader.GetGuid(reader.GetOrdinal("TimePeriodId")),
                    PeriodStart = reader.GetDateTime(reader.GetOrdinal("PeriodStart")),
                    PeriodEnd = reader.GetDateTime(reader.GetOrdinal("PeriodEnd")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    SubmittedDate = reader.IsDBNull(reader.GetOrdinal("SubmittedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("SubmittedDate")),
                    ExpectedDeliveryDate = reader.IsDBNull(reader.GetOrdinal("ExpectedDeliveryDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ExpectedDeliveryDate")),
                    BillableHours = reader.GetDecimal(reader.GetOrdinal("BillableHours")),
                    InternalHours = reader.GetDecimal(reader.GetOrdinal("InternalHours")),
                    EquityHours = reader.GetDecimal(reader.GetOrdinal("EquityHours")),
                    Gross = reader.IsDBNull(reader.GetOrdinal("Gross")) ? null : reader["Gross"].ToString(),
                    Net = reader.IsDBNull(reader.GetOrdinal("Net")) ? null : reader["Net"].ToString(),
                    Expenses = reader.IsDBNull(reader.GetOrdinal("Expenses")) ? null : reader["Expenses"].ToString(),
                    PrimaryTransactionId = reader.IsDBNull(reader.GetOrdinal("PrimaryTransactionId")) ? null : reader["PrimaryTransactionId"].ToString(),
                    SecondaryTransactionId = reader.IsDBNull(reader.GetOrdinal("SecondaryTransactionId")) ? null : reader["SecondaryTransactionId"].ToString(),
                    PrimaryDeposit = reader.IsDBNull(reader.GetOrdinal("PrimaryDeposit")) ? null : reader["PrimaryDeposit"].ToString(),
                    EstimatedDeposit = reader.IsDBNull(reader.GetOrdinal("EstimatedDeposit")) ? null : reader["EstimatedDeposit"].ToString(),
                    ExpenseDetail = reader.IsDBNull(reader.GetOrdinal("ExpenseDetail")) ? null : reader["ExpenseDetail"].ToString(),
                    DeductionsDetail = reader.IsDBNull(reader.GetOrdinal("DeductionsDetail")) ? null : reader["DeductionsDetail"].ToString(),
                    EarnedEquity = reader.IsDBNull(reader.GetOrdinal("EarnedEquity")) ? null : reader["EarnedEquity"].ToString(),
                    ContractorPayment = reader.GetBoolean(reader.GetOrdinal("ContractorPayment")),
                    W2Payment = reader.GetBoolean(reader.GetOrdinal("W2Payment")),
                    OfficierPayment = reader.GetBoolean(reader.GetOrdinal("OfficierPayment")),
                });
            }

            return rows;
        }
    }
}
