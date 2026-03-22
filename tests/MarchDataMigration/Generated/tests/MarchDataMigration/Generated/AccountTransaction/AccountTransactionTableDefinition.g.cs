using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.AccountTransaction
{
    // generated: target table contract
    public static class AccountTransactionTableDefinition
    {
        public const string TableName = "dbo.AccountTransaction";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("AccountId", typeof(Guid));
            table.Columns.Add("TransactionDate", typeof(DateTime));
            table.Columns.Add("EncryptedAmount", typeof(string));
            table.Columns.Add("IsReconciled", typeof(bool));
            table.Columns.Add("TransactionCategoryId", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Tag", typeof(string));
            table.Columns.Add("OriginalHash", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("VendorId", typeof(Guid));
            return table;
        }

        public static void AddRow(DataTable table, TargetAccountTransactionRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.AccountId,
                row.TransactionDate,
                (object?)row.EncryptedAmount ?? DBNull.Value,
                row.IsReconciled,
                row.TransactionCategoryId,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                (object?)row.Tag ?? DBNull.Value,
                (object?)row.OriginalHash ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.VendorId.HasValue ? row.VendorId.Value : DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("AccountId", "AccountId");
            bulk.ColumnMappings.Add("TransactionDate", "TransactionDate");
            bulk.ColumnMappings.Add("EncryptedAmount", "EncryptedAmount");
            bulk.ColumnMappings.Add("IsReconciled", "IsReconciled");
            bulk.ColumnMappings.Add("TransactionCategoryId", "TransactionCategoryId");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("Tag", "Tag");
            bulk.ColumnMappings.Add("OriginalHash", "OriginalHash");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("VendorId", "VendorId");
        }
    }
}
