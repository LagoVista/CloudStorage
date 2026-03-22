using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Org
{
    // generated: target table contract
    public static class OrgTableDefinition
    {
        public const string TableName = "dbo.Org";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("OrgId", typeof(string));
            table.Columns.Add("OrgName", typeof(string));
            table.Columns.Add("OrgBillingContactId", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetOrgRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                (object?)row.OrgId ?? DBNull.Value,
                (object?)row.OrgName ?? DBNull.Value,
                (object?)row.OrgBillingContactId ?? DBNull.Value,
                (object?)row.Status ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("OrgId", "OrgId");
            bulk.ColumnMappings.Add("OrgName", "OrgName");
            bulk.ColumnMappings.Add("OrgBillingContactId", "OrgBillingContactId");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
        }
    }
}
