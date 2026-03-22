using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductIncluded
{
    // generated: full source query and reader
    public static class ProductIncludedSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    PackageId,
    ProductId,
    DIscount,
    Notes,
    Name,
    [Key],
    Quantity
FROM dbo.ProductIncluded
ORDER BY Id;";

        public static async Task<List<SourceProductIncludedRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceProductIncludedRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceProductIncludedRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    PackageId = reader.GetGuid(reader.GetOrdinal("PackageId")),
                    ProductId = reader.GetGuid(reader.GetOrdinal("ProductId")),
                    DIscount = reader.GetDecimal(reader.GetOrdinal("DIscount")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                });
            }

            return rows;
        }
    }
}
