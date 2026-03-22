using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.ProductIncluded
{
    // generated: target table contract
    public static class ProductIncludedTableDefinition
    {
        public const string TableName = "dbo.ProductIncluded";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("PackageId", typeof(Guid));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("DiscountPercent", typeof(decimal));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            return table;
        }

        public static void AddRow(DataTable table, TargetProductIncludedRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.PackageId,
                row.ProductId,
                row.DiscountPercent,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                row.Quantity);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("PackageId", "PackageId");
            bulk.ColumnMappings.Add("ProductId", "ProductId");
            bulk.ColumnMappings.Add("DiscountPercent", "DiscountPercent");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Quantity", "Quantity");
        }
    }
}
