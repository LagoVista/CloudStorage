using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Invoice
{
    // generated: full source query and reader
    public static class InvoiceSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    IsMaster,
    HasChildren,
    MasterInvoiceId,
    InvoiceNumber,
    SubscriptionId,
    OrgId,
    CustomerId,
    Notes,
    BillingStart,
    BillingEnd,
    CreationTimeStamp,
    DueDate,
    Total,
    Discount,
    Extended,
    TotalPaid,
    PaidDate,
    ClosedTransactionId,
    Status,
    StatusDate,
    FailedAttemptCount,
    AgreementId,
    Shipping,
    Tax,
    Subtotal,
    TaxPercent,
    ContactId,
    AdditionalNotes,
    IsLocked
FROM dbo.Invoice
ORDER BY Id;";

        public static async Task<List<SourceInvoiceRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceInvoiceRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceInvoiceRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    IsMaster = reader.GetBoolean(reader.GetOrdinal("IsMaster")),
                    HasChildren = reader.GetBoolean(reader.GetOrdinal("HasChildren")),
                    MasterInvoiceId = reader.IsDBNull(reader.GetOrdinal("MasterInvoiceId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("MasterInvoiceId")),
                    InvoiceNumber = reader.GetInt32(reader.GetOrdinal("InvoiceNumber")),
                    SubscriptionId = reader.IsDBNull(reader.GetOrdinal("SubscriptionId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("SubscriptionId")),
                    OrgId = reader.IsDBNull(reader.GetOrdinal("OrgId")) ? null : reader["OrgId"].ToString(),
                    CustomerId = reader.IsDBNull(reader.GetOrdinal("CustomerId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("CustomerId")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    BillingStart = reader.GetDateTime(reader.GetOrdinal("BillingStart")),
                    BillingEnd = reader.GetDateTime(reader.GetOrdinal("BillingEnd")),
                    CreationTimeStamp = reader.GetDateTime(reader.GetOrdinal("CreationTimeStamp")),
                    DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    Total = reader.IsDBNull(reader.GetOrdinal("Total")) ? null : reader["Total"].ToString(),
                    Discount = reader.IsDBNull(reader.GetOrdinal("Discount")) ? null : reader["Discount"].ToString(),
                    Extended = reader.IsDBNull(reader.GetOrdinal("Extended")) ? null : reader["Extended"].ToString(),
                    TotalPaid = reader.IsDBNull(reader.GetOrdinal("TotalPaid")) ? null : reader["TotalPaid"].ToString(),
                    PaidDate = reader.IsDBNull(reader.GetOrdinal("PaidDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("PaidDate")),
                    ClosedTransactionId = reader.IsDBNull(reader.GetOrdinal("ClosedTransactionId")) ? null : reader["ClosedTransactionId"].ToString(),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    StatusDate = reader.GetDateTime(reader.GetOrdinal("StatusDate")),
                    FailedAttemptCount = reader.GetInt32(reader.GetOrdinal("FailedAttemptCount")),
                    AgreementId = reader.IsDBNull(reader.GetOrdinal("AgreementId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("AgreementId")),
                    Shipping = reader.IsDBNull(reader.GetOrdinal("Shipping")) ? null : reader["Shipping"].ToString(),
                    Tax = reader.IsDBNull(reader.GetOrdinal("Tax")) ? null : reader["Tax"].ToString(),
                    Subtotal = reader.IsDBNull(reader.GetOrdinal("Subtotal")) ? null : reader["Subtotal"].ToString(),
                    TaxPercent = reader.GetDecimal(reader.GetOrdinal("TaxPercent")),
                    ContactId = reader.IsDBNull(reader.GetOrdinal("ContactId")) ? null : reader["ContactId"].ToString(),
                    AdditionalNotes = reader.IsDBNull(reader.GetOrdinal("AdditionalNotes")) ? null : reader["AdditionalNotes"].ToString(),
                    IsLocked = reader.GetBoolean(reader.GetOrdinal("IsLocked")),
                });
            }

            return rows;
        }
    }
}
