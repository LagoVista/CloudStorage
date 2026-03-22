using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.InvoiceLineItems
{
    // generated: full source query and reader
    public static class InvoiceLineItemsSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    InvoiceId,
    AgreementId,
    ResourceId,
    ResourceName,
    ProductName,
    Quantity,
    Units,
    UnitPrice,
    Total,
    Discount,
    Extended,
    Taxable,
    ProductId,
    Shipping
FROM dbo.InvoiceLineItems
ORDER BY Id;";

        public static async Task<List<SourceInvoiceLineItemsRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceInvoiceLineItemsRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceInvoiceLineItemsRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    InvoiceId = reader.GetGuid(reader.GetOrdinal("InvoiceId")),
                    AgreementId = reader.IsDBNull(reader.GetOrdinal("AgreementId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("AgreementId")),
                    ResourceId = reader.IsDBNull(reader.GetOrdinal("ResourceId")) ? null : reader["ResourceId"].ToString(),
                    ResourceName = reader.IsDBNull(reader.GetOrdinal("ResourceName")) ? null : reader["ResourceName"].ToString(),
                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader["ProductName"].ToString(),
                    Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
                    Units = reader.IsDBNull(reader.GetOrdinal("Units")) ? null : reader["Units"].ToString(),
                    UnitPrice = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? null : reader["UnitPrice"].ToString(),
                    Total = reader.IsDBNull(reader.GetOrdinal("Total")) ? null : reader["Total"].ToString(),
                    Discount = reader.IsDBNull(reader.GetOrdinal("Discount")) ? null : reader["Discount"].ToString(),
                    Extended = reader.IsDBNull(reader.GetOrdinal("Extended")) ? null : reader["Extended"].ToString(),
                    Taxable = reader.IsDBNull(reader.GetOrdinal("Taxable")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("Taxable")),
                    ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ProductId")),
                    Shipping = reader.IsDBNull(reader.GetOrdinal("Shipping")) ? null : reader["Shipping"].ToString(),
                });
            }

            return rows;
        }
    }
}
