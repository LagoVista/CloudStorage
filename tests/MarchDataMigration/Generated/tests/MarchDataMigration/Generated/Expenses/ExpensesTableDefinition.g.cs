using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Expenses
{
    // generated: target table contract
    public static class ExpensesTableDefinition
    {
        public const string TableName = "dbo.Expenses";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("TimePeriodId", typeof(Guid));
            table.Columns.Add("CreditCardId", typeof(Guid));
            table.Columns.Add("ExpenseCategoryId", typeof(Guid));
            table.Columns.Add("AgreementId", typeof(Guid));
            table.Columns.Add("BillingEventId", typeof(Guid));
            table.Columns.Add("PaymentId", typeof(Guid));
            table.Columns.Add("ExpenseDate", typeof(DateTime));
            table.Columns.Add("ProjectId", typeof(string));
            table.Columns.Add("ProjectName", typeof(string));
            table.Columns.Add("WorkTaskId", typeof(string));
            table.Columns.Add("WorkTaskName", typeof(string));
            table.Columns.Add("UserId", typeof(string));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("Approved", typeof(bool));
            table.Columns.Add("ApprovedById", typeof(string));
            table.Columns.Add("ApprovedDate", typeof(DateTime));
            table.Columns.Add("Locked", typeof(bool));
            table.Columns.Add("EncryptedAmount", typeof(string));
            table.Columns.Add("EncryptedReimbursedAmount", typeof(string));
            table.Columns.Add("Notes", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("VendorId", typeof(Guid));
            return table;
        }

        public static void AddRow(DataTable table, TargetExpensesRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                row.TimePeriodId,
                row.CreditCardId.HasValue ? row.CreditCardId.Value : DBNull.Value,
                row.ExpenseCategoryId,
                row.AgreementId.HasValue ? row.AgreementId.Value : DBNull.Value,
                row.BillingEventId.HasValue ? row.BillingEventId.Value : DBNull.Value,
                row.PaymentId.HasValue ? row.PaymentId.Value : DBNull.Value,
                row.ExpenseDate,
                (object?)row.ProjectId ?? DBNull.Value,
                (object?)row.ProjectName ?? DBNull.Value,
                (object?)row.WorkTaskId ?? DBNull.Value,
                (object?)row.WorkTaskName ?? DBNull.Value,
                (object?)row.UserId ?? DBNull.Value,
                (object?)row.OrganizationId ?? DBNull.Value,
                row.Approved,
                (object?)row.ApprovedById ?? DBNull.Value,
                row.ApprovedDate.HasValue ? row.ApprovedDate.Value : DBNull.Value,
                row.Locked,
                (object?)row.EncryptedAmount ?? DBNull.Value,
                (object?)row.EncryptedReimbursedAmount ?? DBNull.Value,
                (object?)row.Notes ?? DBNull.Value,
                (object?)row.Description ?? DBNull.Value,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                row.VendorId.HasValue ? row.VendorId.Value : DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("TimePeriodId", "TimePeriodId");
            bulk.ColumnMappings.Add("CreditCardId", "CreditCardId");
            bulk.ColumnMappings.Add("ExpenseCategoryId", "ExpenseCategoryId");
            bulk.ColumnMappings.Add("AgreementId", "AgreementId");
            bulk.ColumnMappings.Add("BillingEventId", "BillingEventId");
            bulk.ColumnMappings.Add("PaymentId", "PaymentId");
            bulk.ColumnMappings.Add("ExpenseDate", "ExpenseDate");
            bulk.ColumnMappings.Add("ProjectId", "ProjectId");
            bulk.ColumnMappings.Add("ProjectName", "ProjectName");
            bulk.ColumnMappings.Add("WorkTaskId", "WorkTaskId");
            bulk.ColumnMappings.Add("WorkTaskName", "WorkTaskName");
            bulk.ColumnMappings.Add("UserId", "UserId");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("Approved", "Approved");
            bulk.ColumnMappings.Add("ApprovedById", "ApprovedById");
            bulk.ColumnMappings.Add("ApprovedDate", "ApprovedDate");
            bulk.ColumnMappings.Add("Locked", "Locked");
            bulk.ColumnMappings.Add("EncryptedAmount", "EncryptedAmount");
            bulk.ColumnMappings.Add("EncryptedReimbursedAmount", "EncryptedReimbursedAmount");
            bulk.ColumnMappings.Add("Notes", "Notes");
            bulk.ColumnMappings.Add("Description", "Description");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("VendorId", "VendorId");
        }
    }
}
