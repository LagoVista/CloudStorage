using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.RecurringCycleType
{
    // generated: target table contract
    public static class RecurringCycleTypeTableDefinition
    {
        public const string TableName = "dbo.RecurringCycleType";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Name", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetRecurringCycleTypeRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Name", "Name");
        }
    }
}
