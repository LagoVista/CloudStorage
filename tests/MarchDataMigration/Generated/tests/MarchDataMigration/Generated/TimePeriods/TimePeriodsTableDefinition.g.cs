using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.TimePeriods
{
    // generated: target table contract
    public static class TimePeriodsTableDefinition
    {
        public const string TableName = "dbo.TimePeriods";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Year", typeof(int));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Locked", typeof(bool));
            table.Columns.Add("LockedByUserId", typeof(string));
            table.Columns.Add("LockedTimestamp", typeof(DateTime));
            table.Columns.Add("PayrollRunId", typeof(Guid));
            table.Columns.Add("Start", typeof(DateTime));
            table.Columns.Add("End", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetTimePeriodsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.Year,
                (object?)row.OrganizationId ?? DBNull.Value,
                row.Locked,
                (object?)row.LockedByUserId ?? DBNull.Value,
                row.LockedTimestamp.HasValue ? row.LockedTimestamp.Value : DBNull.Value,
                row.PayrollRunId.HasValue ? row.PayrollRunId.Value : DBNull.Value,
                row.Start,
                row.End);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("Year", "Year");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Locked", "Locked");
            bulk.ColumnMappings.Add("LockedByUserId", "LockedByUserId");
            bulk.ColumnMappings.Add("LockedTimestamp", "LockedTimestamp");
            bulk.ColumnMappings.Add("PayrollRunId", "PayrollRunId");
            bulk.ColumnMappings.Add("Start", "Start");
            bulk.ColumnMappings.Add("End", "End");
        }
    }
}
