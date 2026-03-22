using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.ProductPage_Product
{
    // generated: target table contract
    public static class ProductPage_ProductTableDefinition
    {
        public const string TableName = "dbo.ProductPage_Product";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("ProductPageId", typeof(Guid));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("Discount", typeof(decimal));
            table.Columns.Add("Index", typeof(int));
            table.Columns.Add("UnitQty", typeof(int));
            return table;
        }

        public static void AddRow(DataTable table, TargetProductPage_ProductRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.ProductPageId,
                row.ProductId,
                row.Discount,
                row.Index,
                row.UnitQty);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("ProductPageId", "ProductPageId");
            bulk.ColumnMappings.Add("ProductId", "ProductId");
            bulk.ColumnMappings.Add("Discount", "Discount");
            bulk.ColumnMappings.Add("Index", "Index");
            bulk.ColumnMappings.Add("UnitQty", "UnitQty");
        }
    }
}
