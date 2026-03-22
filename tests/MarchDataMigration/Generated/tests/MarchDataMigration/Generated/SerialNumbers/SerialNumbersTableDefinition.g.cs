using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.SerialNumbers
{
    // generated: target table contract
    public static class SerialNumbersTableDefinition
    {
        public const string TableName = "dbo.SerialNumbers";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Index", typeof(int));
            table.Columns.Add("OrgId", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("KeyId", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetSerialNumbersRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Index,
                (object?)row.OrgId ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.KeyId ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Index", "Index");
            bulk.ColumnMappings.Add("OrgId", "OrgId");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("KeyId", "KeyId");
        }
    }
}
