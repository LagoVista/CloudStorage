using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Product
{
    // generated: full source query and reader
    public static class ProductSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    ProductCategoryId,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    [Key],
    Name,
    Sku,
    Status,
    UnitCost,
    UnitTypeId,
    Description,
    DetailsHTML,
    RemoteResourceId,
    IsTrialResource,
    icon,
    ThumbnailImageResourceId,
    ThumbnailImageResourceName,
    ImageResourceId,
    ImageResourceName,
    PhysicalProduct,
    ShortSummaryHTML,
    UnitPrice,
    IsPublic,
    RecurringCycleTypeId
FROM dbo.Product
ORDER BY Id;";

        public static async Task<List<SourceProductRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceProductRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceProductRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    ProductCategoryId = reader.GetGuid(reader.GetOrdinal("ProductCategoryId")),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Sku = reader.IsDBNull(reader.GetOrdinal("Sku")) ? null : reader["Sku"].ToString(),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader["Status"].ToString(),
                    UnitCost = reader.GetDecimal(reader.GetOrdinal("UnitCost")),
                    UnitTypeId = reader.GetInt32(reader.GetOrdinal("UnitTypeId")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    DetailsHTML = reader.IsDBNull(reader.GetOrdinal("DetailsHTML")) ? null : reader["DetailsHTML"].ToString(),
                    RemoteResourceId = reader.IsDBNull(reader.GetOrdinal("RemoteResourceId")) ? null : reader["RemoteResourceId"].ToString(),
                    IsTrialResource = reader.GetBoolean(reader.GetOrdinal("IsTrialResource")),
                    icon = reader.IsDBNull(reader.GetOrdinal("icon")) ? null : reader["icon"].ToString(),
                    ThumbnailImageResourceId = reader.IsDBNull(reader.GetOrdinal("ThumbnailImageResourceId")) ? null : reader["ThumbnailImageResourceId"].ToString(),
                    ThumbnailImageResourceName = reader.IsDBNull(reader.GetOrdinal("ThumbnailImageResourceName")) ? null : reader["ThumbnailImageResourceName"].ToString(),
                    ImageResourceId = reader.IsDBNull(reader.GetOrdinal("ImageResourceId")) ? null : reader["ImageResourceId"].ToString(),
                    ImageResourceName = reader.IsDBNull(reader.GetOrdinal("ImageResourceName")) ? null : reader["ImageResourceName"].ToString(),
                    PhysicalProduct = reader.GetBoolean(reader.GetOrdinal("PhysicalProduct")),
                    ShortSummaryHTML = reader.IsDBNull(reader.GetOrdinal("ShortSummaryHTML")) ? null : reader["ShortSummaryHTML"].ToString(),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                    RecurringCycleTypeId = reader.GetInt32(reader.GetOrdinal("RecurringCycleTypeId")),
                });
            }

            return rows;
        }
    }
}
