using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductPage_Product
{
    // generated: full source query and reader
    public static class ProductPage_ProductSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    ProductPageId,
    ProductId,
    Discount,
    [Index],
    UnitQty
FROM dbo.ProductPage_Product
ORDER BY Id;";

        public static async Task<List<SourceProductPage_ProductRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceProductPage_ProductRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceProductPage_ProductRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    ProductPageId = reader.GetGuid(reader.GetOrdinal("ProductPageId")),
                    ProductId = reader.GetGuid(reader.GetOrdinal("ProductId")),
                    Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                    Index = reader.GetInt32(reader.GetOrdinal("Index")),
                    UnitQty = reader.GetInt32(reader.GetOrdinal("UnitQty")),
                });
            }

            return rows;
        }
    }
}
