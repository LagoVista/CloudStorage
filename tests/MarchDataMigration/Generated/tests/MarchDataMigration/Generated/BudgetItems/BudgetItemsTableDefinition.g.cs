using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.BudgetItems
{
    // generated: target table contract
    public static class BudgetItemsTableDefinition
    {
        public const string TableName = "dbo.BudgetItems";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("Year", typeof(int));
            table.Columns.Add("Month", typeof(int));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("AccountTransactionCategoryId", typeof(Guid));
            table.Columns.Add("ExpenseCategoryId", typeof(Guid));
            table.Columns.Add("WorkRoleId", typeof(Guid));
            table.Columns.Add("EncryptedAllocated", typeof(string));
            table.Columns.Add("EncryptedActual", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("Description", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetBudgetItemsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Icon ?? DBNull.Value,
                row.Year,
                row.Month,
                (object?)row.OrganizationId ?? DBNull.Value,
                row.AccountTransactionCategoryId.HasValue ? row.AccountTransactionCategoryId.Value : DBNull.Value,
                row.ExpenseCategoryId.HasValue ? row.ExpenseCategoryId.Value : DBNull.Value,
                row.WorkRoleId.HasValue ? row.WorkRoleId.Value : DBNull.Value,
                (object?)row.EncryptedAllocated ?? DBNull.Value,
                (object?)row.EncryptedActual ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                (object?)row.Description ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("Year", "Year");
            bulk.ColumnMappings.Add("Month", "Month");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("AccountTransactionCategoryId", "AccountTransactionCategoryId");
            bulk.ColumnMappings.Add("ExpenseCategoryId", "ExpenseCategoryId");
            bulk.ColumnMappings.Add("WorkRoleId", "WorkRoleId");
            bulk.ColumnMappings.Add("EncryptedAllocated", "EncryptedAllocated");
            bulk.ColumnMappings.Add("EncryptedActual", "EncryptedActual");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("Description", "Description");
        }
    }
}
