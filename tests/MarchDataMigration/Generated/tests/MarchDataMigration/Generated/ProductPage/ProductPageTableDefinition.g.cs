using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.ProductPage
{
    // generated: target table contract
    public static class ProductPageTableDefinition
    {
        public const string TableName = "dbo.ProductPage";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("PageTitle", typeof(string));
            table.Columns.Add("ShortSummaryHTML", typeof(string));
            table.Columns.Add("ThumbnailImageResourceId", typeof(string));
            table.Columns.Add("ThumbnailImageResourceName", typeof(string));
            table.Columns.Add("ImageResourceId", typeof(string));
            table.Columns.Add("ImageResourceName", typeof(string));
            table.Columns.Add("HeroImageResourceId", typeof(string));
            table.Columns.Add("HeroImageResourceName", typeof(string));
            table.Columns.Add("HeroTitle", typeof(string));
            table.Columns.Add("HeroTagLine1", typeof(string));
            table.Columns.Add("HeroTagLine2", typeof(string));
            table.Columns.Add("TopLeftMenuId", typeof(string));
            table.Columns.Add("TopLeftMenuName", typeof(string));
            table.Columns.Add("TopRightMenuId", typeof(string));
            table.Columns.Add("TopRightMenuName", typeof(string));
            table.Columns.Add("BottomMenuId", typeof(string));
            table.Columns.Add("BottomMenuName", typeof(string));
            table.Columns.Add("ColorPaletteId", typeof(string));
            table.Columns.Add("ColorPaletteName", typeof(string));
            table.Columns.Add("ProductPageLayoutId", typeof(string));
            table.Columns.Add("ProductPageLayoutName", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("IsPublic", typeof(bool));
            table.Columns.Add("DescriptionHtml", typeof(string));
            table.Columns.Add("VideoUrl", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetProductPageRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Icon ?? DBNull.Value,
                (object?)row.PageTitle ?? DBNull.Value,
                (object?)row.ShortSummaryHTML ?? DBNull.Value,
                (object?)row.ThumbnailImageResourceId ?? DBNull.Value,
                (object?)row.ThumbnailImageResourceName ?? DBNull.Value,
                (object?)row.ImageResourceId ?? DBNull.Value,
                (object?)row.ImageResourceName ?? DBNull.Value,
                (object?)row.HeroImageResourceId ?? DBNull.Value,
                (object?)row.HeroImageResourceName ?? DBNull.Value,
                (object?)row.HeroTitle ?? DBNull.Value,
                (object?)row.HeroTagLine1 ?? DBNull.Value,
                (object?)row.HeroTagLine2 ?? DBNull.Value,
                (object?)row.TopLeftMenuId ?? DBNull.Value,
                (object?)row.TopLeftMenuName ?? DBNull.Value,
                (object?)row.TopRightMenuId ?? DBNull.Value,
                (object?)row.TopRightMenuName ?? DBNull.Value,
                (object?)row.BottomMenuId ?? DBNull.Value,
                (object?)row.BottomMenuName ?? DBNull.Value,
                (object?)row.ColorPaletteId ?? DBNull.Value,
                (object?)row.ColorPaletteName ?? DBNull.Value,
                (object?)row.ProductPageLayoutId ?? DBNull.Value,
                (object?)row.ProductPageLayoutName ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.IsPublic,
                (object?)row.DescriptionHtml ?? DBNull.Value,
                (object?)row.VideoUrl ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("PageTitle", "PageTitle");
            bulk.ColumnMappings.Add("ShortSummaryHTML", "ShortSummaryHTML");
            bulk.ColumnMappings.Add("ThumbnailImageResourceId", "ThumbnailImageResourceId");
            bulk.ColumnMappings.Add("ThumbnailImageResourceName", "ThumbnailImageResourceName");
            bulk.ColumnMappings.Add("ImageResourceId", "ImageResourceId");
            bulk.ColumnMappings.Add("ImageResourceName", "ImageResourceName");
            bulk.ColumnMappings.Add("HeroImageResourceId", "HeroImageResourceId");
            bulk.ColumnMappings.Add("HeroImageResourceName", "HeroImageResourceName");
            bulk.ColumnMappings.Add("HeroTitle", "HeroTitle");
            bulk.ColumnMappings.Add("HeroTagLine1", "HeroTagLine1");
            bulk.ColumnMappings.Add("HeroTagLine2", "HeroTagLine2");
            bulk.ColumnMappings.Add("TopLeftMenuId", "TopLeftMenuId");
            bulk.ColumnMappings.Add("TopLeftMenuName", "TopLeftMenuName");
            bulk.ColumnMappings.Add("TopRightMenuId", "TopRightMenuId");
            bulk.ColumnMappings.Add("TopRightMenuName", "TopRightMenuName");
            bulk.ColumnMappings.Add("BottomMenuId", "BottomMenuId");
            bulk.ColumnMappings.Add("BottomMenuName", "BottomMenuName");
            bulk.ColumnMappings.Add("ColorPaletteId", "ColorPaletteId");
            bulk.ColumnMappings.Add("ColorPaletteName", "ColorPaletteName");
            bulk.ColumnMappings.Add("ProductPageLayoutId", "ProductPageLayoutId");
            bulk.ColumnMappings.Add("ProductPageLayoutName", "ProductPageLayoutName");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("IsPublic", "IsPublic");
            bulk.ColumnMappings.Add("DescriptionHtml", "DescriptionHtml");
            bulk.ColumnMappings.Add("VideoUrl", "VideoUrl");
        }
    }
}
