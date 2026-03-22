using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductCategory
{
    // generated: full source query and reader
    public static class ProductCategorySourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    OrgId,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    Name,
    [Key],
    Description,
    IsPublic,
    icon,
    ThumbnailImageResourceId,
    ThumbnailImageResourceName,
    ImageResourceId,
    ImageResourceName,
    ShortSummaryHTML,
    CategoryTypeId,
    CategoryTypeName
FROM dbo.ProductCategory
ORDER BY Id;";

        public static async Task<List<SourceProductCategoryRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceProductCategoryRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceProductCategoryRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    OrgId = reader.IsDBNull(reader.GetOrdinal("OrgId")) ? null : reader["OrgId"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                    icon = reader.IsDBNull(reader.GetOrdinal("icon")) ? null : reader["icon"].ToString(),
                    ThumbnailImageResourceId = reader.IsDBNull(reader.GetOrdinal("ThumbnailImageResourceId")) ? null : reader["ThumbnailImageResourceId"].ToString(),
                    ThumbnailImageResourceName = reader.IsDBNull(reader.GetOrdinal("ThumbnailImageResourceName")) ? null : reader["ThumbnailImageResourceName"].ToString(),
                    ImageResourceId = reader.IsDBNull(reader.GetOrdinal("ImageResourceId")) ? null : reader["ImageResourceId"].ToString(),
                    ImageResourceName = reader.IsDBNull(reader.GetOrdinal("ImageResourceName")) ? null : reader["ImageResourceName"].ToString(),
                    ShortSummaryHTML = reader.IsDBNull(reader.GetOrdinal("ShortSummaryHTML")) ? null : reader["ShortSummaryHTML"].ToString(),
                    CategoryTypeId = reader.IsDBNull(reader.GetOrdinal("CategoryTypeId")) ? null : reader["CategoryTypeId"].ToString(),
                    CategoryTypeName = reader.IsDBNull(reader.GetOrdinal("CategoryTypeName")) ? null : reader["CategoryTypeName"].ToString(),
                });
            }

            return rows;
        }
    }
}
