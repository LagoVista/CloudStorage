using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MarchDataMigration.Generated.Payments
{
    // generated: target table contract
    public static class PaymentsTableDefinition
    {
        public const string TableName = "dbo.Payments";

        public static DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("CreatedById", typeof(string));
            table.Columns.Add("LastUpdatedById", typeof(string));
            table.Columns.Add("CreationDate", typeof(DateTime));
            table.Columns.Add("LastUpdatedDate", typeof(DateTime));
            table.Columns.Add("UserId", typeof(string));
            table.Columns.Add("TimePeriodId", typeof(Guid));
            table.Columns.Add("PayrollRunId", typeof(Guid));
            table.Columns.Add("PeriodStart", typeof(DateTime));
            table.Columns.Add("PeriodEnd", typeof(DateTime));
            table.Columns.Add("PaymentStatus", typeof(string));
            table.Columns.Add("OrganizationId", typeof(string));
            table.Columns.Add("SubmittedDate", typeof(DateTime));
            table.Columns.Add("ExpectedDeliveryDate", typeof(DateTime));
            table.Columns.Add("BillableHours", typeof(decimal));
            table.Columns.Add("InternalHours", typeof(decimal));
            table.Columns.Add("EquityHours", typeof(decimal));
            table.Columns.Add("EncryptedGross", typeof(string));
            table.Columns.Add("EncryptedNet", typeof(string));
            table.Columns.Add("EncryptedExpenses", typeof(string));
            table.Columns.Add("PrimaryTransactionId", typeof(string));
            table.Columns.Add("SecondaryTransactionId", typeof(string));
            table.Columns.Add("EncryptedPrimaryDeposit", typeof(string));
            table.Columns.Add("EncryptedEstimatedTaxWithholding", typeof(string));
            table.Columns.Add("ExpenseDetail", typeof(string));
            table.Columns.Add("DeductionsDetail", typeof(string));
            table.Columns.Add("EncryptedEarnedEquity", typeof(string));
            table.Columns.Add("ContractorPayment", typeof(bool));
            table.Columns.Add("W2Payment", typeof(bool));
            table.Columns.Add("OfficerPayment", typeof(bool));
            table.Columns.Add("EncryptedSecondaryDeposit", typeof(string));
            return table;
        }

        public static void AddRow(DataTable table, TargetPaymentsRow row)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(row);

            table.Rows.Add(
                row.Id,
                (object?)row.CreatedById ?? DBNull.Value,
                (object?)row.LastUpdatedById ?? DBNull.Value,
                row.CreationDate,
                row.LastUpdatedDate,
                (object?)row.UserId ?? DBNull.Value,
                row.TimePeriodId,
                row.PayrollRunId,
                row.PeriodStart,
                row.PeriodEnd,
                (object?)row.PaymentStatus ?? DBNull.Value,
                (object?)row.OrganizationId ?? DBNull.Value,
                row.SubmittedDate.HasValue ? row.SubmittedDate.Value : DBNull.Value,
                row.ExpectedDeliveryDate.HasValue ? row.ExpectedDeliveryDate.Value : DBNull.Value,
                row.BillableHours,
                row.InternalHours,
                row.EquityHours,
                (object?)row.EncryptedGross ?? DBNull.Value,
                (object?)row.EncryptedNet ?? DBNull.Value,
                (object?)row.EncryptedExpenses ?? DBNull.Value,
                (object?)row.PrimaryTransactionId ?? DBNull.Value,
                (object?)row.SecondaryTransactionId ?? DBNull.Value,
                (object?)row.EncryptedPrimaryDeposit ?? DBNull.Value,
                (object?)row.EncryptedEstimatedTaxWithholding ?? DBNull.Value,
                (object?)row.ExpenseDetail ?? DBNull.Value,
                (object?)row.DeductionsDetail ?? DBNull.Value,
                (object?)row.EncryptedEarnedEquity ?? DBNull.Value,
                row.ContractorPayment,
                row.W2Payment,
                row.OfficerPayment,
                (object?)row.EncryptedSecondaryDeposit ?? DBNull.Value);
        }

        public static void ConfigureMappings(SqlBulkCopy bulk)
        {
            ArgumentNullException.ThrowIfNull(bulk);

            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("CreatedById", "CreatedById");
            bulk.ColumnMappings.Add("LastUpdatedById", "LastUpdatedById");
            bulk.ColumnMappings.Add("CreationDate", "CreationDate");
            bulk.ColumnMappings.Add("LastUpdatedDate", "LastUpdatedDate");
            bulk.ColumnMappings.Add("UserId", "UserId");
            bulk.ColumnMappings.Add("TimePeriodId", "TimePeriodId");
            bulk.ColumnMappings.Add("PayrollRunId", "PayrollRunId");
            bulk.ColumnMappings.Add("PeriodStart", "PeriodStart");
            bulk.ColumnMappings.Add("PeriodEnd", "PeriodEnd");
            bulk.ColumnMappings.Add("PaymentStatus", "PaymentStatus");
            bulk.ColumnMappings.Add("OrganizationId", "OrganizationId");
            bulk.ColumnMappings.Add("SubmittedDate", "SubmittedDate");
            bulk.ColumnMappings.Add("ExpectedDeliveryDate", "ExpectedDeliveryDate");
            bulk.ColumnMappings.Add("BillableHours", "BillableHours");
            bulk.ColumnMappings.Add("InternalHours", "InternalHours");
            bulk.ColumnMappings.Add("EquityHours", "EquityHours");
            bulk.ColumnMappings.Add("EncryptedGross", "EncryptedGross");
            bulk.ColumnMappings.Add("EncryptedNet", "EncryptedNet");
            bulk.ColumnMappings.Add("EncryptedExpenses", "EncryptedExpenses");
            bulk.ColumnMappings.Add("PrimaryTransactionId", "PrimaryTransactionId");
            bulk.ColumnMappings.Add("SecondaryTransactionId", "SecondaryTransactionId");
            bulk.ColumnMappings.Add("EncryptedPrimaryDeposit", "EncryptedPrimaryDeposit");
            bulk.ColumnMappings.Add("EncryptedEstimatedTaxWithholding", "EncryptedEstimatedTaxWithholding");
            bulk.ColumnMappings.Add("ExpenseDetail", "ExpenseDetail");
            bulk.ColumnMappings.Add("DeductionsDetail", "DeductionsDetail");
            bulk.ColumnMappings.Add("EncryptedEarnedEquity", "EncryptedEarnedEquity");
            bulk.ColumnMappings.Add("ContractorPayment", "ContractorPayment");
            bulk.ColumnMappings.Add("W2Payment", "W2Payment");
            bulk.ColumnMappings.Add("OfficerPayment", "OfficerPayment");
            bulk.ColumnMappings.Add("EncryptedSecondaryDeposit", "EncryptedSecondaryDeposit");
        }
    }
}
