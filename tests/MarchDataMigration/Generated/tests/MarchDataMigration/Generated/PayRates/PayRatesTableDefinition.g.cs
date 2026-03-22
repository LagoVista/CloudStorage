using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.PayRates
{
    // generated: target table contract
    public static class PayRatesTableDefinition
    {
        public const string TableName = "dbo.PayRates";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("UserId", typeof(string));
            table.Columns.Add("Start", typeof(DateTime));
            table.Columns.Add("End", typeof(DateTime));
            table.Columns.Add("IsSalary", typeof(bool));
            table.Columns.Add("FilingType", typeof(string));
            table.Columns.Add("DeductEstimated", typeof(bool));
            table.Columns.Add("DeductEstimatedRate", typeof(decimal));
            table.Columns.Add("EncryptedBillableRate", typeof(string));
            table.Columns.Add("EncryptedInternalRate", typeof(string));
            table.Columns.Add("EncryptedSalary", typeof(string));
            table.Columns.Add("EncryptedDeductions", typeof(string));
            table.Columns.Add("EncryptedEquityScaler", typeof(string));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("WorkRoleId", typeof(Guid));
            table.Columns.Add("IsContractor", typeof(bool));
            table.Columns.Add("IsFTE", typeof(bool));
            table.Columns.Add("IsOfficer", typeof(bool));
            return table;
        }

        public static void AddRow(DataTable table, TargetPayRatesRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.UserId ?? DBNull.Value,
                row.Start,
                row.End.HasValue ? row.End.Value : DBNull.Value,
                row.IsSalary,
                (object?)row.FilingType ?? DBNull.Value,
                row.DeductEstimated,
                row.DeductEstimatedRate,
                (object?)row.EncryptedBillableRate ?? DBNull.Value,
                (object?)row.EncryptedInternalRate ?? DBNull.Value,
                (object?)row.EncryptedSalary ?? DBNull.Value,
                (object?)row.EncryptedDeductions ?? DBNull.Value,
                (object?)row.EncryptedEquityScaler ?? DBNull.Value,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.WorkRoleId.HasValue ? row.WorkRoleId.Value : DBNull.Value,
                row.IsContractor,
                row.IsFTE,
                row.IsOfficer);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("UserId", "UserId");
            bulk.ColumnMappings.Add("Start", "Start");
            bulk.ColumnMappings.Add("End", "End");
            bulk.ColumnMappings.Add("IsSalary", "IsSalary");
            bulk.ColumnMappings.Add("FilingType", "FilingType");
            bulk.ColumnMappings.Add("DeductEstimated", "DeductEstimated");
            bulk.ColumnMappings.Add("DeductEstimatedRate", "DeductEstimatedRate");
            bulk.ColumnMappings.Add("EncryptedBillableRate", "EncryptedBillableRate");
            bulk.ColumnMappings.Add("EncryptedInternalRate", "EncryptedInternalRate");
            bulk.ColumnMappings.Add("EncryptedSalary", "EncryptedSalary");
            bulk.ColumnMappings.Add("EncryptedDeductions", "EncryptedDeductions");
            bulk.ColumnMappings.Add("EncryptedEquityScaler", "EncryptedEquityScaler");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("WorkRoleId", "WorkRoleId");
            bulk.ColumnMappings.Add("IsContractor", "IsContractor");
            bulk.ColumnMappings.Add("IsFTE", "IsFTE");
            bulk.ColumnMappings.Add("IsOfficer", "IsOfficer");
        }
    }
}
