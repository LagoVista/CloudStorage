using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.AgreementLineItems
{
    // generated: full source query and reader
    public static class AgreementLineItemsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    AgreementId,
    ProductId,
    ProductName,
    Start,
    [End],
    UnitPrice,
    DiscountPercent,
    Extended,
    SubTotal,
    Quantity,
    UnitTypeId,
    IsRecurring,
    RecurringCycleTypeId,
    NextBillingDate,
    LastBilledDate,
    Taxable,
    Shipping
FROM dbo.AgreementLineItems
ORDER BY Id;";

        public static async Task<List<SourceAgreementLineItemsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceAgreementLineItemsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceAgreementLineItemsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    AgreementId = reader.GetGuid(reader.GetOrdinal("AgreementId")),
                    ProductId = reader.GetGuid(reader.GetOrdinal("ProductId")),
                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader["ProductName"].ToString(),
                    Start = reader.IsDBNull(reader.GetOrdinal("Start")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Start")),
                    End = reader.IsDBNull(reader.GetOrdinal("End")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("End")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    DiscountPercent = reader.GetDecimal(reader.GetOrdinal("DiscountPercent")),
                    Extended = reader.GetDecimal(reader.GetOrdinal("Extended")),
                    SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                    Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
                    UnitTypeId = reader.GetInt32(reader.GetOrdinal("UnitTypeId")),
                    IsRecurring = reader.GetBoolean(reader.GetOrdinal("IsRecurring")),
                    RecurringCycleTypeId = reader.IsDBNull(reader.GetOrdinal("RecurringCycleTypeId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("RecurringCycleTypeId")),
                    NextBillingDate = reader.IsDBNull(reader.GetOrdinal("NextBillingDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("NextBillingDate")),
                    LastBilledDate = reader.IsDBNull(reader.GetOrdinal("LastBilledDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastBilledDate")),
                    Taxable = reader.GetBoolean(reader.GetOrdinal("Taxable")),
                    Shipping = reader.GetDecimal(reader.GetOrdinal("Shipping")),
                });
            }

            return rows;
        }
    }
}
