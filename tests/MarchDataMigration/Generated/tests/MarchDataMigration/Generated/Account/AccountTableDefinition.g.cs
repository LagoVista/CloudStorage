using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Account
{
    // generated: target table contract
    public static class AccountTableDefinition
    {
        public const string TableName = "dbo.Account";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("EncryptedRoutingNumber", typeof(string));
            table.Columns.Add("EncryptedAccountNumber", typeof(string));
            table.Columns.Add("Institution", typeof(string));
            table.Columns.Add("IsLiability", typeof(bool));
            table.Columns.Add("EncryptedBalance", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("TransactionJournalOnly", typeof(bool));
            table.Columns.Add("EncryptedOnlineBalance", typeof(string));
            table.Columns.Add("Version", typeof(long));
            return table;
        }

        public static void AddRow(DataTable table, TargetAccountRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.EncryptedRoutingNumber ?? DBNull.Value,
                (object?)row.EncryptedAccountNumber ?? DBNull.Value,
                (object?)row.Institution ?? DBNull.Value,
                row.IsLiability,
                (object?)row.EncryptedBalance ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.IsActive,
                row.TransactionJournalOnly,
                (object?)row.EncryptedOnlineBalance ?? DBNull.Value,
                row.Version);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("EncryptedRoutingNumber", "EncryptedRoutingNumber");
            bulk.ColumnMappings.Add("EncryptedAccountNumber", "EncryptedAccountNumber");
            bulk.ColumnMappings.Add("Institution", "Institution");
            bulk.ColumnMappings.Add("IsLiability", "IsLiability");
            bulk.ColumnMappings.Add("EncryptedBalance", "EncryptedBalance");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("IsActive", "IsActive");
            bulk.ColumnMappings.Add("TransactionJournalOnly", "TransactionJournalOnly");
            bulk.ColumnMappings.Add("EncryptedOnlineBalance", "EncryptedOnlineBalance");
            bulk.ColumnMappings.Add("Version", "Version");
        }
    }
}
