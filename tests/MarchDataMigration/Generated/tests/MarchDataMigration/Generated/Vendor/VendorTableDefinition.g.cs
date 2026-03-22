using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Vendor
{
    // generated: target table contract
    public static class VendorTableDefinition
    {
        public const string TableName = "dbo.Vendor";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("DefaultExpenseCategoryId", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("MaxAmount", typeof(decimal));
            table.Columns.Add("PayPeriod", typeof(string));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("Contact", typeof(string));
            table.Columns.Add("Phone", typeof(string));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("Address1", typeof(string));
            table.Columns.Add("Address2", typeof(string));
            table.Columns.Add("City", typeof(string));
            table.Columns.Add("StateOrProvince", typeof(string));
            table.Columns.Add("PostalCode", typeof(string));
            table.Columns.Add("Country", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("DefaultAccountTransactionCategoryId", typeof(Guid));
            return table;
        }

        public static void AddRow(DataTable table, TargetVendorRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.OrganizationId ?? DBNull.Value,
                row.DefaultExpenseCategoryId,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                row.MaxAmount,
                (object?)row.PayPeriod ?? DBNull.Value,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.Contact ?? DBNull.Value,
                (object?)row.Phone ?? DBNull.Value,
                (object?)row.Icon ?? DBNull.Value,
                (object?)row.Address1 ?? DBNull.Value,
                (object?)row.Address2 ?? DBNull.Value,
                (object?)row.City ?? DBNull.Value,
                (object?)row.StateOrProvince ?? DBNull.Value,
                (object?)row.PostalCode ?? DBNull.Value,
                (object?)row.Country ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.IsActive,
                row.DefaultAccountTransactionCategoryId.HasValue ? row.DefaultAccountTransactionCategoryId.Value : DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("DefaultExpenseCategoryId", "DefaultExpenseCategoryId");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("MaxAmount", "MaxAmount");
            bulk.ColumnMappings.Add("PayPeriod", "PayPeriod");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("Contact", "Contact");
            bulk.ColumnMappings.Add("Phone", "Phone");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("Address1", "Address1");
            bulk.ColumnMappings.Add("Address2", "Address2");
            bulk.ColumnMappings.Add("City", "City");
            bulk.ColumnMappings.Add("StateOrProvince", "StateOrProvince");
            bulk.ColumnMappings.Add("PostalCode", "PostalCode");
            bulk.ColumnMappings.Add("Country", "Country");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("IsActive", "IsActive");
            bulk.ColumnMappings.Add("DefaultAccountTransactionCategoryId", "DefaultAccountTransactionCategoryId");
        }
    }
}
