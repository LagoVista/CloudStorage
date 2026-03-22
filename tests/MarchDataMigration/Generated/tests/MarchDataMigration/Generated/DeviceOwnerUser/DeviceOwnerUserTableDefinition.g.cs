using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.DeviceOwnerUser
{
    // generated: target table contract
    public static class DeviceOwnerUserTableDefinition
    {
        public const string TableName = "dbo.DeviceOwnerUser";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("DeviceOwnerUserId", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Phone", typeof(string));
            table.Columns.Add("FullName", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetDeviceOwnerUserRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                (object?)row.DeviceOwnerUserId ?? DBNull.Value,
                (object?)row.Email ?? DBNull.Value,
                (object?)row.Phone ?? DBNull.Value,
                (object?)row.FullName ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("DeviceOwnerUserId", "DeviceOwnerUserId");
            bulk.ColumnMappings.Add("Email", "Email");
            bulk.ColumnMappings.Add("Phone", "Phone");
            bulk.ColumnMappings.Add("FullName", "FullName");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
        }
    }
}
