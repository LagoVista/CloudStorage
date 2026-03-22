using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Customers
{
    // generated: target table contract
    public static class CustomersTableDefinition
    {
        public const string TableName = "dbo.Customers";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("CustomerName", typeof(string));
            table.Columns.Add("BillingContactName", typeof(string));
            table.Columns.Add("BillingContactEmail", typeof(string));
            table.Columns.Add("Address1", typeof(string));
            table.Columns.Add("Address2", typeof(string));
            table.Columns.Add("City", typeof(string));
            table.Columns.Add("State", typeof(string));
            table.Columns.Add("Zip", typeof(string));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetCustomersRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.CustomerName ?? DBNull.Value,
                (object?)row.BillingContactName ?? DBNull.Value,
                (object?)row.BillingContactEmail ?? DBNull.Value,
                (object?)row.Address1 ?? DBNull.Value,
                (object?)row.Address2 ?? DBNull.Value,
                (object?)row.City ?? DBNull.Value,
                (object?)row.State ?? DBNull.Value,
                (object?)row.Zip ?? DBNull.Value,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("CustomerName", "CustomerName");
            bulk.ColumnMappings.Add("BillingContactName", "BillingContactName");
            bulk.ColumnMappings.Add("BillingContactEmail", "BillingContactEmail");
            bulk.ColumnMappings.Add("Address1", "Address1");
            bulk.ColumnMappings.Add("Address2", "Address2");
            bulk.ColumnMappings.Add("City", "City");
            bulk.ColumnMappings.Add("State", "State");
            bulk.ColumnMappings.Add("Zip", "Zip");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
        }
    }
}
