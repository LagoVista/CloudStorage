using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.TimeEntries
{
    // generated: target table contract
    public static class TimeEntriesTableDefinition
    {
        public const string TableName = "dbo.TimeEntries";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("AgreementId", typeof(Guid));
            table.Columns.Add("TimePeriodId", typeof(Guid));
            table.Columns.Add("BillingEventId", typeof(Guid));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("ProjectId", typeof(string));
            table.Columns.Add("ProjectName", typeof(string));
            table.Columns.Add("WorkTaskId", typeof(string));
            table.Columns.Add("WorkTaskName", typeof(string));
            table.Columns.Add("UserId", typeof(string));
            table.Columns.Add("Locked", typeof(bool));
            table.Columns.Add("IsEquityTime", typeof(bool));
            table.Columns.Add("Hours", typeof(decimal));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            return table;
        }

        public static void AddRow(DataTable table, TargetTimeEntriesRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.AgreementId,
                row.TimePeriodId,
                row.BillingEventId.HasValue ? row.BillingEventId.Value : DBNull.Value,
                row.Date,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.ProjectId ?? DBNull.Value,
                (object?)row.ProjectName ?? DBNull.Value,
                (object?)row.WorkTaskId ?? DBNull.Value,
                (object?)row.WorkTaskName ?? DBNull.Value,
                (object?)row.UserId ?? DBNull.Value,
                row.Locked,
                row.IsEquityTime,
                row.Hours,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("AgreementId", "AgreementId");
            bulk.ColumnMappings.Add("TimePeriodId", "TimePeriodId");
            bulk.ColumnMappings.Add("BillingEventId", "BillingEventId");
            bulk.ColumnMappings.Add("Date", "Date");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("ProjectId", "ProjectId");
            bulk.ColumnMappings.Add("ProjectName", "ProjectName");
            bulk.ColumnMappings.Add("WorkTaskId", "WorkTaskId");
            bulk.ColumnMappings.Add("WorkTaskName", "WorkTaskName");
            bulk.ColumnMappings.Add("UserId", "UserId");
            bulk.ColumnMappings.Add("Locked", "Locked");
            bulk.ColumnMappings.Add("IsEquityTime", "IsEquityTime");
            bulk.ColumnMappings.Add("Hours", "Hours");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
        }
    }
}
