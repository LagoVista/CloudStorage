using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.BillingEvents
{
    // generated: target table contract
    public static class BillingEventsTableDefinition
    {
        public const string TableName = "dbo.BillingEvents";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("ResourceId", typeof(string));
            table.Columns.Add("ResourceName", typeof(string));
            table.Columns.Add("SubscriptionId", typeof(Guid));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("StartTimestamp", typeof(DateTime));
            table.Columns.Add("StartedByAppUserId", typeof(string));
            table.Columns.Add("EndTimestamp", typeof(DateTime));
            table.Columns.Add("RolloverAt", typeof(DateTime));
            table.Columns.Add("BillingTimeZoneId", typeof(int));
            table.Columns.Add("BillingDate", typeof(DateTime));
            table.Columns.Add("EndedByAppUserId", typeof(string));
            table.Columns.Add("HoursBilled", typeof(decimal));
            table.Columns.Add("UnitCost", typeof(decimal));
            table.Columns.Add("DiscountPercent", typeof(decimal));
            table.Columns.Add("Extended", typeof(decimal));
            table.Columns.Add("UnitTypeId", typeof(int));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("Tokens", typeof(long));
            return table;
        }

        public static void AddRow(DataTable table, TargetBillingEventsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.ResourceId ?? DBNull.Value,
                (object?)row.ResourceName ?? DBNull.Value,
                row.SubscriptionId,
                row.ProductId,
                row.StartTimestamp,
                (object?)row.StartedByAppUserId ?? DBNull.Value,
                row.EndTimestamp.HasValue ? row.EndTimestamp.Value : DBNull.Value,
                row.RolloverAt.HasValue ? row.RolloverAt.Value : DBNull.Value,
                row.BillingTimeZoneId,
                row.BillingDate.HasValue ? row.BillingDate.Value : DBNull.Value,
                (object?)row.EndedByAppUserId ?? DBNull.Value,
                row.HoursBilled.HasValue ? row.HoursBilled.Value : DBNull.Value,
                row.UnitCost.HasValue ? row.UnitCost.Value : DBNull.Value,
                row.DiscountPercent.HasValue ? row.DiscountPercent.Value : DBNull.Value,
                row.Extended.HasValue ? row.Extended.Value : DBNull.Value,
                row.UnitTypeId,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.Status ?? DBNull.Value,
                row.UnitPrice.HasValue ? row.UnitPrice.Value : DBNull.Value,
                row.Tokens.HasValue ? row.Tokens.Value : DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("ResourceId", "ResourceId");
            bulk.ColumnMappings.Add("ResourceName", "ResourceName");
            bulk.ColumnMappings.Add("SubscriptionId", "SubscriptionId");
            bulk.ColumnMappings.Add("ProductId", "ProductId");
            bulk.ColumnMappings.Add("StartTimestamp", "StartTimestamp");
            bulk.ColumnMappings.Add("StartedByAppUserId", "StartedByAppUserId");
            bulk.ColumnMappings.Add("EndTimestamp", "EndTimestamp");
            bulk.ColumnMappings.Add("RolloverAt", "RolloverAt");
            bulk.ColumnMappings.Add("BillingTimeZoneId", "BillingTimeZoneId");
            bulk.ColumnMappings.Add("BillingDate", "BillingDate");
            bulk.ColumnMappings.Add("EndedByAppUserId", "EndedByAppUserId");
            bulk.ColumnMappings.Add("HoursBilled", "HoursBilled");
            bulk.ColumnMappings.Add("UnitCost", "UnitCost");
            bulk.ColumnMappings.Add("DiscountPercent", "DiscountPercent");
            bulk.ColumnMappings.Add("Extended", "Extended");
            bulk.ColumnMappings.Add("UnitTypeId", "UnitTypeId");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("UnitPrice", "UnitPrice");
            bulk.ColumnMappings.Add("Tokens", "Tokens");
        }
    }
}
