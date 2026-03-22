using MarchDataMigration.Generated.PayrollRun;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.PayrollSummary
{
    // generated: target table contract
    public static class PayrollSummaryTableDefinition
    {
        public const string TableName = "dbo.PayrollRun";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("EncryptedTotalSalary", typeof(string));
            table.Columns.Add("EncryptedTotalGross", typeof(string));
            table.Columns.Add("EncryptedTotalNet", typeof(string));
            table.Columns.Add("EncryptedTotalPayroll", typeof(string));
            table.Columns.Add("EncryptedTotalExpenses", typeof(string));
            table.Columns.Add("EncryptedTotalPayrollTaxObligation", typeof(string));
            table.Columns.Add("EncryptedTotalRevenue", typeof(string));
            table.Columns.Add("EncryptedTaxLiabilities", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Locked", typeof(bool));
            table.Columns.Add("LockedTimestamp", typeof(DateTime));
            table.Columns.Add("LockedByUserId", typeof(string));
            table.Columns.Add("Approved", typeof(bool));
            table.Columns.Add("ApprovedTimestamp", typeof(DateTime));
            table.Columns.Add("ApprovedByUserId", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetPayrollRunRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                (object?)row.OrganizationId ?? DBNull.Value,
                (object?)row.EncryptedTotalSalary ?? DBNull.Value,
                (object?)row.EncryptedTotalGross ?? DBNull.Value,
                (object?)row.EncryptedTotalNet ?? DBNull.Value,
                (object?)row.EncryptedTotalPayroll ?? DBNull.Value,
                (object?)row.EncryptedTotalExpenses ?? DBNull.Value,
                (object?)row.EncryptedTotalPayrollTaxObligation ?? DBNull.Value,
                (object?)row.EncryptedTotalRevenue ?? DBNull.Value,
                (object?)row.EncryptedTaxLiabilities ?? DBNull.Value,
                (object?)row.Status ?? DBNull.Value,
                row.Locked,
                row.LockedTimestamp.HasValue ? row.LockedTimestamp.Value : DBNull.Value,
                (object?)row.LockedByUserId ?? DBNull.Value,
                row.Approved,
                row.ApprovedTimestamp.HasValue ? row.ApprovedTimestamp.Value : DBNull.Value,
                (object?)row.ApprovedByUserId ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("EncryptedTotalSalary", "EncryptedTotalSalary");
            bulk.ColumnMappings.Add("EncryptedTotalGross", "EncryptedTotalGross");
            bulk.ColumnMappings.Add("EncryptedTotalNet", "EncryptedTotalNet");
            bulk.ColumnMappings.Add("EncryptedTotalPayroll", "EncryptedTotalPayroll");
            bulk.ColumnMappings.Add("EncryptedTotalExpenses", "EncryptedTotalExpenses");
            bulk.ColumnMappings.Add("EncryptedTotalPayrollTaxObligation", "EncryptedTotalPayrollTaxObligation");
            bulk.ColumnMappings.Add("EncryptedTotalRevenue", "EncryptedTotalRevenue");
            bulk.ColumnMappings.Add("EncryptedTaxLiabilities", "EncryptedTaxLiabilities");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("Locked", "Locked");
            bulk.ColumnMappings.Add("LockedTimestamp", "LockedTimestamp");
            bulk.ColumnMappings.Add("LockedByUserId", "LockedByUserId");
            bulk.ColumnMappings.Add("Approved", "Approved");
            bulk.ColumnMappings.Add("ApprovedTimestamp", "ApprovedTimestamp");
            bulk.ColumnMappings.Add("ApprovedByUserId", "ApprovedByUserId");
        }
    }
}
