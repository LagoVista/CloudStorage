using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Subscription
{
    // generated: target table contract
    public static class SubscriptionTableDefinition
    {
        public const string TableName = "dbo.Subscription";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("CustomerId", typeof(Guid));
            table.Columns.Add("PaymentTokenCustomerId", typeof(string));
            table.Columns.Add("PaymentTokenSecretId", typeof(string));
            table.Columns.Add("PaymentTokenDate", typeof(DateTime));
            table.Columns.Add("PaymentTokenExpires", typeof(DateTime));
            table.Columns.Add("PaymentTokenStatus", typeof(string));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("Start", typeof(DateTime));
            table.Columns.Add("End", typeof(DateTime));
            table.Columns.Add("PaymentAccountId", typeof(string));
            table.Columns.Add("PaymentAccountType", typeof(string));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("ActiveDate", typeof(DateTime));
            table.Columns.Add("InactiveDate", typeof(DateTime));
            table.Columns.Add("TrialStartDate", typeof(DateTime));
            table.Columns.Add("TrialExpirationDate", typeof(DateTime));
            table.Columns.Add("IsTrial", typeof(bool));
            return table;
        }

        public static void AddRow(DataTable table, TargetSubscriptionRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Status ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                row.CustomerId.HasValue ? row.CustomerId.Value : DBNull.Value,
                (object?)row.PaymentTokenCustomerId ?? DBNull.Value,
                (object?)row.PaymentTokenSecretId ?? DBNull.Value,
                row.PaymentTokenDate.HasValue ? row.PaymentTokenDate.Value : DBNull.Value,
                row.PaymentTokenExpires.HasValue ? row.PaymentTokenExpires.Value : DBNull.Value,
                (object?)row.PaymentTokenStatus ?? DBNull.Value,
                (object?)row.Icon ?? DBNull.Value,
                row.Start,
                row.End.HasValue ? row.End.Value : DBNull.Value,
                (object?)row.PaymentAccountId ?? DBNull.Value,
                (object?)row.PaymentAccountType ?? DBNull.Value,
                row.IsActive,
                row.ActiveDate.HasValue ? row.ActiveDate.Value : DBNull.Value,
                row.InactiveDate.HasValue ? row.InactiveDate.Value : DBNull.Value,
                row.TrialStartDate.HasValue ? row.TrialStartDate.Value : DBNull.Value,
                row.TrialExpirationDate.HasValue ? row.TrialExpirationDate.Value : DBNull.Value,
                row.IsTrial);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("CustomerId", "CustomerId");
            bulk.ColumnMappings.Add("PaymentTokenCustomerId", "PaymentTokenCustomerId");
            bulk.ColumnMappings.Add("PaymentTokenSecretId", "PaymentTokenSecretId");
            bulk.ColumnMappings.Add("PaymentTokenDate", "PaymentTokenDate");
            bulk.ColumnMappings.Add("PaymentTokenExpires", "PaymentTokenExpires");
            bulk.ColumnMappings.Add("PaymentTokenStatus", "PaymentTokenStatus");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("Start", "Start");
            bulk.ColumnMappings.Add("End", "End");
            bulk.ColumnMappings.Add("PaymentAccountId", "PaymentAccountId");
            bulk.ColumnMappings.Add("PaymentAccountType", "PaymentAccountType");
            bulk.ColumnMappings.Add("IsActive", "IsActive");
            bulk.ColumnMappings.Add("ActiveDate", "ActiveDate");
            bulk.ColumnMappings.Add("InactiveDate", "InactiveDate");
            bulk.ColumnMappings.Add("TrialStartDate", "TrialStartDate");
            bulk.ColumnMappings.Add("TrialExpirationDate", "TrialExpirationDate");
            bulk.ColumnMappings.Add("IsTrial", "IsTrial");
        }
    }
}
