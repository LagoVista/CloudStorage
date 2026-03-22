using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.InvoiceLineItems
{
    // generated: target table contract
    public static class InvoiceLineItemsTableDefinition
    {
        public const string TableName = "dbo.InvoiceLineItems";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("InvoiceId", typeof(Guid));
            table.Columns.Add("CustomerId", typeof(Guid));
            table.Columns.Add("AgreementId", typeof(Guid));
            table.Columns.Add("ResourceId", typeof(string));
            table.Columns.Add("ResourceName", typeof(string));
            table.Columns.Add("ProductName", typeof(string));
            table.Columns.Add("Quantity", typeof(decimal));
            table.Columns.Add("Units", typeof(string));
            table.Columns.Add("EncryptedUnitPrice", typeof(string));
            table.Columns.Add("EncryptedTotal", typeof(string));
            table.Columns.Add("EncryptedDiscount", typeof(string));
            table.Columns.Add("EncryptedExtended", typeof(string));
            table.Columns.Add("Taxable", typeof(bool));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("EncryptedShipping", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetInvoiceLineItemsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.InvoiceId,
                row.CustomerId,
                row.AgreementId.HasValue ? row.AgreementId.Value : DBNull.Value,
                (object?)row.ResourceId ?? DBNull.Value,
                (object?)row.ResourceName ?? DBNull.Value,
                (object?)row.ProductName ?? DBNull.Value,
                row.Quantity,
                (object?)row.Units ?? DBNull.Value,
                (object?)row.EncryptedUnitPrice ?? DBNull.Value,
                (object?)row.EncryptedTotal ?? DBNull.Value,
                (object?)row.EncryptedDiscount ?? DBNull.Value,
                (object?)row.EncryptedExtended ?? DBNull.Value,
                row.Taxable.HasValue ? row.Taxable.Value : DBNull.Value,
                row.ProductId.HasValue ? row.ProductId.Value : DBNull.Value,
                (object?)row.EncryptedShipping ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("InvoiceId", "InvoiceId");
            bulk.ColumnMappings.Add("CustomerId", "CustomerId");
            bulk.ColumnMappings.Add("AgreementId", "AgreementId");
            bulk.ColumnMappings.Add("ResourceId", "ResourceId");
            bulk.ColumnMappings.Add("ResourceName", "ResourceName");
            bulk.ColumnMappings.Add("ProductName", "ProductName");
            bulk.ColumnMappings.Add("Quantity", "Quantity");
            bulk.ColumnMappings.Add("Units", "Units");
            bulk.ColumnMappings.Add("EncryptedUnitPrice", "EncryptedUnitPrice");
            bulk.ColumnMappings.Add("EncryptedTotal", "EncryptedTotal");
            bulk.ColumnMappings.Add("EncryptedDiscount", "EncryptedDiscount");
            bulk.ColumnMappings.Add("EncryptedExtended", "EncryptedExtended");
            bulk.ColumnMappings.Add("Taxable", "Taxable");
            bulk.ColumnMappings.Add("ProductId", "ProductId");
            bulk.ColumnMappings.Add("EncryptedShipping", "EncryptedShipping");
        }
    }
}
