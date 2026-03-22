using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.InvoiceLogs
{
    // generated: target table contract
    public static class InvoiceLogsTableDefinition
    {
        public const string TableName = "dbo.InvoiceLogs";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("InvoiceId", typeof(Guid));
            table.Columns.Add("CustomerId", typeof(Guid));
            table.Columns.Add("DateStamp", typeof(DateTime));
            table.Columns.Add("EventId", typeof(string));
            table.Columns.Add("EventData", typeof(string));
            table.Columns.Add("Message", typeof(string));
            table.Columns.Add("EncryptedAmount", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetInvoiceLogsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.InvoiceId,
                row.CustomerId,
                row.DateStamp,
                (object?)row.EventId ?? DBNull.Value,
                (object?)row.EventData ?? DBNull.Value,
                (object?)row.Message ?? DBNull.Value,
                (object?)row.EncryptedAmount ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("InvoiceId", "InvoiceId");
            bulk.ColumnMappings.Add("CustomerId", "CustomerId");
            bulk.ColumnMappings.Add("DateStamp", "DateStamp");
            bulk.ColumnMappings.Add("EventId", "EventId");
            bulk.ColumnMappings.Add("EventData", "EventData");
            bulk.ColumnMappings.Add("Message", "Message");
            bulk.ColumnMappings.Add("EncryptedAmount", "EncryptedAmount");
        }
    }
}
