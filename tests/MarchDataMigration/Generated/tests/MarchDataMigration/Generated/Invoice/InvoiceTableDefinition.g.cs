using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Invoice
{
    // generated: target table contract
    public static class InvoiceTableDefinition
    {
        public const string TableName = "dbo.Invoice";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("IsMaster", typeof(bool));
            table.Columns.Add("MasterInvoiceId", typeof(Guid));
            table.Columns.Add("HasChildren", typeof(bool));
            table.Columns.Add("InvoiceNumber", typeof(int));
            table.Columns.Add("SubscriptionId", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("CustomerId", typeof(Guid));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("BillingStart", typeof(DateTime));
            table.Columns.Add("BillingEnd", typeof(DateTime));
            table.Columns.Add("ServicesStart", typeof(DateTime));
            table.Columns.Add("ServicesEnd", typeof(DateTime));
            table.Columns.Add("CreationTimestamp", typeof(DateTime));
            table.Columns.Add("DueDate", typeof(DateTime));
            table.Columns.Add("EncryptedTotal", typeof(string));
            table.Columns.Add("EncryptedDiscount", typeof(string));
            table.Columns.Add("EncryptedExtended", typeof(string));
            table.Columns.Add("EncryptedTotalPaid", typeof(string));
            table.Columns.Add("PaidDate", typeof(DateTime));
            table.Columns.Add("ClosedTransactionId", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("StatusTimestamp", typeof(DateTime));
            table.Columns.Add("FailedAttemptCount", typeof(int));
            table.Columns.Add("AgreementId", typeof(Guid));
            table.Columns.Add("EncryptedShipping", typeof(string));
            table.Columns.Add("EncryptedTax", typeof(string));
            table.Columns.Add("EncryptedSubtotal", typeof(string));
            table.Columns.Add("TaxPercent", typeof(decimal));
            table.Columns.Add("ContactId", typeof(string));
            table.Columns.Add("ContactName", typeof(string));
            table.Columns.Add("AdditionalNotes", typeof(string));
            table.Columns.Add("IsLocked", typeof(bool));
            table.Columns.Add("InvoiceDate", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetInvoiceRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.IsMaster,
                row.MasterInvoiceId.HasValue ? row.MasterInvoiceId.Value : DBNull.Value,
                row.HasChildren,
                row.InvoiceNumber,
                row.SubscriptionId.HasValue ? row.SubscriptionId.Value : DBNull.Value,
                (object?)row.OrganizationId ?? DBNull.Value,
                row.CustomerId,
                (object?)row.Notes ?? DBNull.Value,
                row.BillingStart,
                row.BillingEnd,
                row.ServicesStart,
                row.ServicesEnd,
                row.CreationTimestamp,
                row.DueDate,
                (object?)row.EncryptedTotal ?? DBNull.Value,
                (object?)row.EncryptedDiscount ?? DBNull.Value,
                (object?)row.EncryptedExtended ?? DBNull.Value,
                (object?)row.EncryptedTotalPaid ?? DBNull.Value,
                row.PaidDate.HasValue ? row.PaidDate.Value : DBNull.Value,
                (object?)row.ClosedTransactionId ?? DBNull.Value,
                (object?)row.Status ?? DBNull.Value,
                row.StatusTimestamp,
                row.FailedAttemptCount,
                row.AgreementId.HasValue ? row.AgreementId.Value : DBNull.Value,
                (object?)row.EncryptedShipping ?? DBNull.Value,
                (object?)row.EncryptedTax ?? DBNull.Value,
                (object?)row.EncryptedSubtotal ?? DBNull.Value,
                row.TaxPercent,
                (object?)row.ContactId ?? DBNull.Value,
                (object?)row.ContactName ?? DBNull.Value,
                (object?)row.AdditionalNotes ?? DBNull.Value,
                row.IsLocked,
                row.InvoiceDate);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("IsMaster", "IsMaster");
            bulk.ColumnMappings.Add("MasterInvoiceId", "MasterInvoiceId");
            bulk.ColumnMappings.Add("HasChildren", "HasChildren");
            bulk.ColumnMappings.Add("InvoiceNumber", "InvoiceNumber");
            bulk.ColumnMappings.Add("SubscriptionId", "SubscriptionId");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("CustomerId", "CustomerId");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("BillingStart", "BillingStart");
            bulk.ColumnMappings.Add("BillingEnd", "BillingEnd");
            bulk.ColumnMappings.Add("ServicesStart", "ServicesStart");
            bulk.ColumnMappings.Add("ServicesEnd", "ServicesEnd");
            bulk.ColumnMappings.Add("CreationTimestamp", "CreationTimestamp");
            bulk.ColumnMappings.Add("DueDate", "DueDate");
            bulk.ColumnMappings.Add("EncryptedTotal", "EncryptedTotal");
            bulk.ColumnMappings.Add("EncryptedDiscount", "EncryptedDiscount");
            bulk.ColumnMappings.Add("EncryptedExtended", "EncryptedExtended");
            bulk.ColumnMappings.Add("EncryptedTotalPaid", "EncryptedTotalPaid");
            bulk.ColumnMappings.Add("PaidDate", "PaidDate");
            bulk.ColumnMappings.Add("ClosedTransactionId", "ClosedTransactionId");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("StatusTimestamp", "StatusTimestamp");
            bulk.ColumnMappings.Add("FailedAttemptCount", "FailedAttemptCount");
            bulk.ColumnMappings.Add("AgreementId", "AgreementId");
            bulk.ColumnMappings.Add("EncryptedShipping", "EncryptedShipping");
            bulk.ColumnMappings.Add("EncryptedTax", "EncryptedTax");
            bulk.ColumnMappings.Add("EncryptedSubtotal", "EncryptedSubtotal");
            bulk.ColumnMappings.Add("TaxPercent", "TaxPercent");
            bulk.ColumnMappings.Add("ContactId", "ContactId");
            bulk.ColumnMappings.Add("ContactName", "ContactName");
            bulk.ColumnMappings.Add("AdditionalNotes", "AdditionalNotes");
            bulk.ColumnMappings.Add("IsLocked", "IsLocked");
            bulk.ColumnMappings.Add("InvoiceDate", "InvoiceDate");
        }
    }
}
