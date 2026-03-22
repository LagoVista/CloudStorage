using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.AppUser
{
    // generated: target table contract
    public static class AppUserTableDefinition
    {
        public const string TableName = "dbo.AppUser";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("AppUserId", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("FullName", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetAppUserRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                (object?)row.AppUserId ?? DBNull.Value,
                (object?)row.Email ?? DBNull.Value,
                (object?)row.FullName ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("AppUserId", "AppUserId");
            bulk.ColumnMappings.Add("Email", "Email");
            bulk.ColumnMappings.Add("FullName", "FullName");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
        }
    }
}
