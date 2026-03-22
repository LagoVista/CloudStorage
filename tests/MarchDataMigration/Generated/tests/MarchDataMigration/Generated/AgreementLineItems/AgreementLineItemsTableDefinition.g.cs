using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.AgreementLineItems
{
    // generated: target table contract
    public static class AgreementLineItemsTableDefinition
    {
        public const string TableName = "dbo.AgreementLineItems";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("AgreementId", typeof(Guid));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("CustomerId", typeof(Guid));
            table.Columns.Add("ProductName", typeof(string));
            table.Columns.Add("Start", typeof(DateTime));
            table.Columns.Add("End", typeof(DateTime));
            table.Columns.Add("EncryptedUnitPrice", typeof(string));
            table.Columns.Add("EncryptedDiscountPercent", typeof(string));
            table.Columns.Add("EncryptedExtended", typeof(string));
            table.Columns.Add("EncryptedSubTotal", typeof(string));
            table.Columns.Add("Quantity", typeof(decimal));
            table.Columns.Add("UnitTypeId", typeof(int));
            table.Columns.Add("IsRecurring", typeof(bool));
            table.Columns.Add("RecurringCycleTypeId", typeof(int));
            table.Columns.Add("NextBillingDate", typeof(DateTime));
            table.Columns.Add("LastBilledDate", typeof(DateTime));
            table.Columns.Add("Taxable", typeof(bool));
            table.Columns.Add("EncryptedShipping", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetAgreementLineItemsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.AgreementId,
                row.ProductId,
                row.CustomerId,
                (object?)row.ProductName ?? DBNull.Value,
                row.Start.HasValue ? row.Start.Value : DBNull.Value,
                row.End.HasValue ? row.End.Value : DBNull.Value,
                (object?)row.EncryptedUnitPrice ?? DBNull.Value,
                (object?)row.EncryptedDiscountPercent ?? DBNull.Value,
                (object?)row.EncryptedExtended ?? DBNull.Value,
                (object?)row.EncryptedSubTotal ?? DBNull.Value,
                row.Quantity,
                row.UnitTypeId,
                row.IsRecurring,
                row.RecurringCycleTypeId.HasValue ? row.RecurringCycleTypeId.Value : DBNull.Value,
                row.NextBillingDate.HasValue ? row.NextBillingDate.Value : DBNull.Value,
                row.LastBilledDate.HasValue ? row.LastBilledDate.Value : DBNull.Value,
                row.Taxable,
                (object?)row.EncryptedShipping ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("AgreementId", "AgreementId");
            bulk.ColumnMappings.Add("ProductId", "ProductId");
            bulk.ColumnMappings.Add("CustomerId", "CustomerId");
            bulk.ColumnMappings.Add("ProductName", "ProductName");
            bulk.ColumnMappings.Add("Start", "Start");
            bulk.ColumnMappings.Add("End", "End");
            bulk.ColumnMappings.Add("EncryptedUnitPrice", "EncryptedUnitPrice");
            bulk.ColumnMappings.Add("EncryptedDiscountPercent", "EncryptedDiscountPercent");
            bulk.ColumnMappings.Add("EncryptedExtended", "EncryptedExtended");
            bulk.ColumnMappings.Add("EncryptedSubTotal", "EncryptedSubTotal");
            bulk.ColumnMappings.Add("Quantity", "Quantity");
            bulk.ColumnMappings.Add("UnitTypeId", "UnitTypeId");
            bulk.ColumnMappings.Add("IsRecurring", "IsRecurring");
            bulk.ColumnMappings.Add("RecurringCycleTypeId", "RecurringCycleTypeId");
            bulk.ColumnMappings.Add("NextBillingDate", "NextBillingDate");
            bulk.ColumnMappings.Add("LastBilledDate", "LastBilledDate");
            bulk.ColumnMappings.Add("Taxable", "Taxable");
            bulk.ColumnMappings.Add("EncryptedShipping", "EncryptedShipping");
        }
    }
}
