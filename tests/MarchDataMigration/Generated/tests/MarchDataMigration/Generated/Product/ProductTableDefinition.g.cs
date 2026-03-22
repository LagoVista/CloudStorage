using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Product
{
    // generated: target table contract
    public static class ProductTableDefinition
    {
        public const string TableName = "dbo.Product";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("ProductCategoryId", typeof(Guid));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Sku", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("UnitCost", typeof(decimal));
            table.Columns.Add("UnitTypeId", typeof(int));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("DetailsHTML", typeof(string));
            table.Columns.Add("RemoteResourceId", typeof(string));
            table.Columns.Add("IsTrialResource", typeof(bool));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("ThumbnailImageResourceId", typeof(string));
            table.Columns.Add("ThumbnailImageResourceName", typeof(string));
            table.Columns.Add("ImageResourceId", typeof(string));
            table.Columns.Add("ImageResourceName", typeof(string));
            table.Columns.Add("PhysicalProduct", typeof(bool));
            table.Columns.Add("ShortSummaryHTML", typeof(string));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("IsPublic", typeof(bool));
            table.Columns.Add("RecurringCycleTypeId", typeof(int));
            return table;
        }

        public static void AddRow(DataTable table, TargetProductRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.ProductCategoryId,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Sku ?? DBNull.Value,
                (object?)row.Status ?? DBNull.Value,
                row.UnitCost,
                row.UnitTypeId,
                (object?)row.Description ?? DBNull.Value,
                (object?)row.DetailsHTML ?? DBNull.Value,
                (object?)row.RemoteResourceId ?? DBNull.Value,
                row.IsTrialResource,
                (object?)row.Icon ?? DBNull.Value,
                (object?)row.ThumbnailImageResourceId ?? DBNull.Value,
                (object?)row.ThumbnailImageResourceName ?? DBNull.Value,
                (object?)row.ImageResourceId ?? DBNull.Value,
                (object?)row.ImageResourceName ?? DBNull.Value,
                row.PhysicalProduct,
                (object?)row.ShortSummaryHTML ?? DBNull.Value,
                row.UnitPrice,
                row.IsPublic,
                row.RecurringCycleTypeId);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("ProductCategoryId", "ProductCategoryId");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Sku", "Sku");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("UnitCost", "UnitCost");
            bulk.ColumnMappings.Add("UnitTypeId", "UnitTypeId");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("DetailsHTML", "DetailsHTML");
            bulk.ColumnMappings.Add("RemoteResourceId", "RemoteResourceId");
            bulk.ColumnMappings.Add("IsTrialResource", "IsTrialResource");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("ThumbnailImageResourceId", "ThumbnailImageResourceId");
            bulk.ColumnMappings.Add("ThumbnailImageResourceName", "ThumbnailImageResourceName");
            bulk.ColumnMappings.Add("ImageResourceId", "ImageResourceId");
            bulk.ColumnMappings.Add("ImageResourceName", "ImageResourceName");
            bulk.ColumnMappings.Add("PhysicalProduct", "PhysicalProduct");
            bulk.ColumnMappings.Add("ShortSummaryHTML", "ShortSummaryHTML");
            bulk.ColumnMappings.Add("UnitPrice", "UnitPrice");
            bulk.ColumnMappings.Add("IsPublic", "IsPublic");
            bulk.ColumnMappings.Add("RecurringCycleTypeId", "RecurringCycleTypeId");
        }
    }
}
