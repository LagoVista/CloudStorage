using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.DeviceOwnerUserDevices
{
    // generated: target table contract
    public static class DeviceOwnerUserDevicesTableDefinition
    {
        public const string TableName = "dbo.DeviceOwnerUserDevices";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("DeviceUniqueId", typeof(string));
            table.Columns.Add("DeviceName", typeof(string));
            table.Columns.Add("DeviceId", typeof(string));
            table.Columns.Add("DeviceOwnerUserId", typeof(string));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("Discount", typeof(decimal));
            return table;
        }

        public static void AddRow(DataTable table, TargetDeviceOwnerUserDevicesRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                (object?)row.Id ?? DBNull.Value,
                (object?)row.DeviceUniqueId ?? DBNull.Value,
                (object?)row.DeviceName ?? DBNull.Value,
                (object?)row.DeviceId ?? DBNull.Value,
                (object?)row.DeviceOwnerUserId ?? DBNull.Value,
                row.ProductId,
                row.Discount);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("DeviceUniqueId", "DeviceUniqueId");
            bulk.ColumnMappings.Add("DeviceName", "DeviceName");
            bulk.ColumnMappings.Add("DeviceId", "DeviceId");
            bulk.ColumnMappings.Add("DeviceOwnerUserId", "DeviceOwnerUserId");
            bulk.ColumnMappings.Add("ProductId", "ProductId");
            bulk.ColumnMappings.Add("Discount", "Discount");
        }
    }
}
