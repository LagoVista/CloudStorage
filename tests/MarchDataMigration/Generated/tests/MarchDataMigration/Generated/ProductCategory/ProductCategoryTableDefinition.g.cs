using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.ProductCategory
{
    // generated: target table contract
    public static class ProductCategoryTableDefinition
    {
        public const string TableName = "dbo.ProductCategory";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("IsPublic", typeof(bool));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("ThumbnailImageResourceId", typeof(string));
            table.Columns.Add("ThumbnailImageResourceName", typeof(string));
            table.Columns.Add("ImageResourceId", typeof(string));
            table.Columns.Add("ImageResourceName", typeof(string));
            table.Columns.Add("ShortSummaryHTML", typeof(string));
            table.Columns.Add("CategoryTypeId", typeof(string));
            table.Columns.Add("CategoryTypeName", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetProductCategoryRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                row.IsPublic,
                (object?)row.Icon ?? DBNull.Value,
                (object?)row.ThumbnailImageResourceId ?? DBNull.Value,
                (object?)row.ThumbnailImageResourceName ?? DBNull.Value,
                (object?)row.ImageResourceId ?? DBNull.Value,
                (object?)row.ImageResourceName ?? DBNull.Value,
                (object?)row.ShortSummaryHTML ?? DBNull.Value,
                (object?)row.CategoryTypeId ?? DBNull.Value,
                (object?)row.CategoryTypeName ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("IsPublic", "IsPublic");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("ThumbnailImageResourceId", "ThumbnailImageResourceId");
            bulk.ColumnMappings.Add("ThumbnailImageResourceName", "ThumbnailImageResourceName");
            bulk.ColumnMappings.Add("ImageResourceId", "ImageResourceId");
            bulk.ColumnMappings.Add("ImageResourceName", "ImageResourceName");
            bulk.ColumnMappings.Add("ShortSummaryHTML", "ShortSummaryHTML");
            bulk.ColumnMappings.Add("CategoryTypeId", "CategoryTypeId");
            bulk.ColumnMappings.Add("CategoryTypeName", "CategoryTypeName");
        }
    }
}
