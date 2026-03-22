using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.ExpenseCategory
{
    // generated: target table contract
    public static class ExpenseCategoryTableDefinition
    {
        public const string TableName = "dbo.ExpenseCategory";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("ReimbursementPercent", typeof(decimal));
            table.Columns.Add("DeductiblePercent", typeof(decimal));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("RequiresApproval", typeof(bool));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("TaxCategory", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetExpenseCategoryRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.CreatedById ?? DBNull.Value,
                row.CreationDate,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.LastUpdatedDate,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                row.ReimbursementPercent,
                row.DeductiblePercent,
                row.IsActive,
                row.RequiresApproval,
                (object?)row.Icon ?? DBNull.Value,
                (object?)row.TaxCategory ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("ReimbursementPercent", "ReimbursementPercent");
            bulk.ColumnMappings.Add("DeductiblePercent", "DeductiblePercent");
            bulk.ColumnMappings.Add("IsActive", "IsActive");
            bulk.ColumnMappings.Add("RequiresApproval", "RequiresApproval");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("TaxCategory", "TaxCategory");
        }
    }
}
