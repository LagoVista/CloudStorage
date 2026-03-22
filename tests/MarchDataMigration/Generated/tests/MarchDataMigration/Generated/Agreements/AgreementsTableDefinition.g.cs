using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Agreements
{
    // generated: target table contract
    public static class AgreementsTableDefinition
    {
        public const string TableName = "dbo.Agreements";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("CustomerId", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Identifier", typeof(string));
            table.Columns.Add("Locked", typeof(bool));
            table.Columns.Add("Internal", typeof(bool));
            table.Columns.Add("InvoicePeriod", typeof(string));
            table.Columns.Add("Terms", typeof(int));
            table.Columns.Add("Start", typeof(DateTime));
            table.Columns.Add("End", typeof(DateTime));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Hours", typeof(decimal));
            table.Columns.Add("EncryptedRate", typeof(string));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("LastInvoicedDate", typeof(DateTime));
            table.Columns.Add("NextInvoiceDate", typeof(DateTime));
            table.Columns.Add("CustomerContactId", typeof(string));
            table.Columns.Add("CustomerContactName", typeof(string));
            table.Columns.Add("EncryptedSubTotal", typeof(string));
            table.Columns.Add("EncryptedDiscountPercent", typeof(string));
            table.Columns.Add("EncryptedTax", typeof(string));
            table.Columns.Add("EncryptedShipping", typeof(string));
            table.Columns.Add("EncryptedTotal", typeof(string));
            table.Columns.Add("TaxPercent", typeof(decimal));
            return table;
        }

        public static void AddRow(DataTable table, TargetAgreementsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.CustomerId,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Identifier ?? DBNull.Value,
                row.Locked,
                row.Internal,
                (object?)row.InvoicePeriod ?? DBNull.Value,
                row.Terms,
                row.Start.HasValue ? row.Start.Value : DBNull.Value,
                row.End.HasValue ? row.End.Value : DBNull.Value,
                (object?)row.Status ?? DBNull.Value,
                row.Hours.HasValue ? row.Hours.Value : DBNull.Value,
                (object?)row.EncryptedRate ?? DBNull.Value,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.LastInvoicedDate.HasValue ? row.LastInvoicedDate.Value : DBNull.Value,
                row.NextInvoiceDate.HasValue ? row.NextInvoiceDate.Value : DBNull.Value,
                (object?)row.CustomerContactId ?? DBNull.Value,
                (object?)row.CustomerContactName ?? DBNull.Value,
                (object?)row.EncryptedSubTotal ?? DBNull.Value,
                (object?)row.EncryptedDiscountPercent ?? DBNull.Value,
                (object?)row.EncryptedTax ?? DBNull.Value,
                (object?)row.EncryptedShipping ?? DBNull.Value,
                (object?)row.EncryptedTotal ?? DBNull.Value,
                row.TaxPercent);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("CustomerId", "CustomerId");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Identifier", "Identifier");
            bulk.ColumnMappings.Add("Locked", "Locked");
            bulk.ColumnMappings.Add("Internal", "Internal");
            bulk.ColumnMappings.Add("InvoicePeriod", "InvoicePeriod");
            bulk.ColumnMappings.Add("Terms", "Terms");
            bulk.ColumnMappings.Add("Start", "Start");
            bulk.ColumnMappings.Add("End", "End");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("Hours", "Hours");
            bulk.ColumnMappings.Add("EncryptedRate", "EncryptedRate");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("LastInvoicedDate", "LastInvoicedDate");
            bulk.ColumnMappings.Add("NextInvoiceDate", "NextInvoiceDate");
            bulk.ColumnMappings.Add("CustomerContactId", "CustomerContactId");
            bulk.ColumnMappings.Add("CustomerContactName", "CustomerContactName");
            bulk.ColumnMappings.Add("EncryptedSubTotal", "EncryptedSubTotal");
            bulk.ColumnMappings.Add("EncryptedDiscountPercent", "EncryptedDiscountPercent");
            bulk.ColumnMappings.Add("EncryptedTax", "EncryptedTax");
            bulk.ColumnMappings.Add("EncryptedShipping", "EncryptedShipping");
            bulk.ColumnMappings.Add("EncryptedTotal", "EncryptedTotal");
            bulk.ColumnMappings.Add("TaxPercent", "TaxPercent");
        }
    }
}
