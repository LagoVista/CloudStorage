using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Agreements
{
    // generated: full source query and reader
    public static class AgreementsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    CustomerId,
    OrganizationId,
    Name,
    Identifier,
    Terms,
    InvoicePeriod,
    Locked,
    Internal,
    Start,
    [End],
    Status,
    Hours,
    EncryptedRate,
    Notes,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    NextInvoiceDate,
    LastInvoicedDate,
    CustomerContactName,
    CustomerContactId,
    SubTotal,
    DiscountPercent,
    Tax,
    Shipping,
    Total,
    TaxPercent
FROM dbo.Agreements
ORDER BY Id;";

        public static async Task<List<SourceAgreementsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceAgreementsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceAgreementsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    CustomerId = reader.GetGuid(reader.GetOrdinal("CustomerId")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Identifier = reader.IsDBNull(reader.GetOrdinal("Identifier")) ? null : reader["Identifier"].ToString(),
                    Terms = reader.GetInt32(reader.GetOrdinal("Terms")),
                    InvoicePeriod = reader.IsDBNull(reader.GetOrdinal("InvoicePeriod")) ? null : reader["InvoicePeriod"].ToString(),
                    Locked = reader.GetBoolean(reader.GetOrdinal("Locked")),
                    Internal = reader.GetBoolean(reader.GetOrdinal("Internal")),
                    Start = reader.IsDBNull(reader.GetOrdinal("Start")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Start")),
                    End = reader.IsDBNull(reader.GetOrdinal("End")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("End")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    Hours = reader.IsDBNull(reader.GetOrdinal("Hours")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("Hours")),
                    EncryptedRate = reader.IsDBNull(reader.GetOrdinal("EncryptedRate")) ? null : reader["EncryptedRate"].ToString(),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    NextInvoiceDate = reader.IsDBNull(reader.GetOrdinal("NextInvoiceDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("NextInvoiceDate")),
                    LastInvoicedDate = reader.IsDBNull(reader.GetOrdinal("LastInvoicedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastInvoicedDate")),
                    CustomerContactName = reader.IsDBNull(reader.GetOrdinal("CustomerContactName")) ? null : reader["CustomerContactName"].ToString(),
                    CustomerContactId = reader.IsDBNull(reader.GetOrdinal("CustomerContactId")) ? null : reader["CustomerContactId"].ToString(),
                    SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                    DiscountPercent = reader.GetDecimal(reader.GetOrdinal("DiscountPercent")),
                    Tax = reader.GetDecimal(reader.GetOrdinal("Tax")),
                    Shipping = reader.GetDecimal(reader.GetOrdinal("Shipping")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                    TaxPercent = reader.GetDecimal(reader.GetOrdinal("TaxPercent")),
                });
            }

            return rows;
        }
    }
}
