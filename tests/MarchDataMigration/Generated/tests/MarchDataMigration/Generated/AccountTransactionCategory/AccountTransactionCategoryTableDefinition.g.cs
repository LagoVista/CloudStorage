using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.AccountTransactionCategory
{
    // generated: target table contract
    public static class AccountTransactionCategoryTableDefinition
    {
        public const string TableName = "dbo.AccountTransactionCategory";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("ExpenseCategoryId", typeof(Guid));
            table.Columns.Add("TaxCategory", typeof(string));
            table.Columns.Add("TaxReportable", typeof(bool));
            table.Columns.Add("Passthrough", typeof(bool));
            return table;
        }

        public static void AddRow(DataTable table, TargetAccountTransactionCategoryRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Type ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.IsActive,
                (object?)row.Icon ?? DBNull.Value,
                row.ExpenseCategoryId.HasValue ? row.ExpenseCategoryId.Value : DBNull.Value,
                (object?)row.TaxCategory ?? DBNull.Value,
                row.TaxReportable,
                row.Passthrough);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Type", "Type");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("IsActive", "IsActive");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("ExpenseCategoryId", "ExpenseCategoryId");
            bulk.ColumnMappings.Add("TaxCategory", "TaxCategory");
            bulk.ColumnMappings.Add("TaxReportable", "TaxReportable");
            bulk.ColumnMappings.Add("Passthrough", "Passthrough");
        }
    }
}
