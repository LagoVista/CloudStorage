using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.ProductPage
{
    // generated: full source query and reader
    public static class ProductPageSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    OrgId,
    Name,
    [Key],
    Icon,
    PageTitle,
    ShortSummaryHTML,
    ThumbnailImageResourceId,
    ThumbnailImageResourceName,
    ImageResourceId,
    ImageResourceName,
    HeroImageResourceId,
    HeroImageResourceName,
    HeroTitle,
    HeroTagLine1,
    HeroTagLine2,
    TopLeftMenuId,
    TopLeftMenuName,
    TopRightMenuId,
    TopRightMenuName,
    BottomMenuId,
    BottomMenuName,
    ColorPaletteId,
    ColorPaletteName,
    ProductPageLayoutId,
    ProductPageLayoutName,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    IsPublic,
    DescriptionHtml,
    VideoUrl
FROM dbo.ProductPage
ORDER BY Id;";

        public static async Task<List<SourceProductPageRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceProductPageRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceProductPageRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    OrgId = reader.IsDBNull(reader.GetOrdinal("OrgId")) ? null : reader["OrgId"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader["Icon"].ToString(),
                    PageTitle = reader.IsDBNull(reader.GetOrdinal("PageTitle")) ? null : reader["PageTitle"].ToString(),
                    ShortSummaryHTML = reader.IsDBNull(reader.GetOrdinal("ShortSummaryHTML")) ? null : reader["ShortSummaryHTML"].ToString(),
                    ThumbnailImageResourceId = reader.IsDBNull(reader.GetOrdinal("ThumbnailImageResourceId")) ? null : reader["ThumbnailImageResourceId"].ToString(),
                    ThumbnailImageResourceName = reader.IsDBNull(reader.GetOrdinal("ThumbnailImageResourceName")) ? null : reader["ThumbnailImageResourceName"].ToString(),
                    ImageResourceId = reader.IsDBNull(reader.GetOrdinal("ImageResourceId")) ? null : reader["ImageResourceId"].ToString(),
                    ImageResourceName = reader.IsDBNull(reader.GetOrdinal("ImageResourceName")) ? null : reader["ImageResourceName"].ToString(),
                    HeroImageResourceId = reader.IsDBNull(reader.GetOrdinal("HeroImageResourceId")) ? null : reader["HeroImageResourceId"].ToString(),
                    HeroImageResourceName = reader.IsDBNull(reader.GetOrdinal("HeroImageResourceName")) ? null : reader["HeroImageResourceName"].ToString(),
                    HeroTitle = reader.IsDBNull(reader.GetOrdinal("HeroTitle")) ? null : reader["HeroTitle"].ToString(),
                    HeroTagLine1 = reader.IsDBNull(reader.GetOrdinal("HeroTagLine1")) ? null : reader["HeroTagLine1"].ToString(),
                    HeroTagLine2 = reader.IsDBNull(reader.GetOrdinal("HeroTagLine2")) ? null : reader["HeroTagLine2"].ToString(),
                    TopLeftMenuId = reader.IsDBNull(reader.GetOrdinal("TopLeftMenuId")) ? null : reader["TopLeftMenuId"].ToString(),
                    TopLeftMenuName = reader.IsDBNull(reader.GetOrdinal("TopLeftMenuName")) ? null : reader["TopLeftMenuName"].ToString(),
                    TopRightMenuId = reader.IsDBNull(reader.GetOrdinal("TopRightMenuId")) ? null : reader["TopRightMenuId"].ToString(),
                    TopRightMenuName = reader.IsDBNull(reader.GetOrdinal("TopRightMenuName")) ? null : reader["TopRightMenuName"].ToString(),
                    BottomMenuId = reader.IsDBNull(reader.GetOrdinal("BottomMenuId")) ? null : reader["BottomMenuId"].ToString(),
                    BottomMenuName = reader.IsDBNull(reader.GetOrdinal("BottomMenuName")) ? null : reader["BottomMenuName"].ToString(),
                    ColorPaletteId = reader.IsDBNull(reader.GetOrdinal("ColorPaletteId")) ? null : reader["ColorPaletteId"].ToString(),
                    ColorPaletteName = reader.IsDBNull(reader.GetOrdinal("ColorPaletteName")) ? null : reader["ColorPaletteName"].ToString(),
                    ProductPageLayoutId = reader.IsDBNull(reader.GetOrdinal("ProductPageLayoutId")) ? null : reader["ProductPageLayoutId"].ToString(),
                    ProductPageLayoutName = reader.IsDBNull(reader.GetOrdinal("ProductPageLayoutName")) ? null : reader["ProductPageLayoutName"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                    DescriptionHtml = reader.IsDBNull(reader.GetOrdinal("DescriptionHtml")) ? null : reader["DescriptionHtml"].ToString(),
                    VideoUrl = reader.IsDBNull(reader.GetOrdinal("VideoUrl")) ? null : reader["VideoUrl"].ToString(),
                });
            }

            return rows;
        }
    }
}
