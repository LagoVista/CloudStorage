using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.WorkRoles
{
    // generated: target table contract
    public static class WorkRolesTableDefinition
    {
        public const string TableName = "dbo.WorkRoles";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Icon", typeof(string));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedById", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetWorkRolesRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.Key ?? DBNull.Value,
                (object?)row.Name ?? DBNull.Value,
                (object?)row.Icon ?? DBNull.Value,
                row.IsActive,
                (object?)row.Description ?? DBNull.Value,
                row.CreationDate,
                (object?)row.CreatedById ?? DBNull.Value,
                row.LastUpdatedDate,
                (object?)row.LastUpdatedById ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Key", "Key");
            bulk.ColumnMappings.Add("Name", "Name");
            bulk.ColumnMappings.Add("Icon", "Icon");
            bulk.ColumnMappings.Add("IsActive", "IsActive");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
        }
    }
}
