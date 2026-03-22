using MarchDataMigration.Generated.ExpenseCategory;
using MarchDataMigration.Generated.Payments;
using System;
using System.Diagnostics.Tracing;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class PaymentsMapper
    {
        public static TargetPaymentsRow Map(SourcePaymentsRow source)
        {
            return new TargetPaymentsRow
            {
                Id = source.Id,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.CreationDate,
                UserId = source.UserId,
                TimePeriodId = source.TimePeriodId,
                PayrollRunId = Guid.Parse("D233CF03-BFC4-4443-85B6-39F1188A7227"),
                PeriodStart = source.PeriodStart,
                PeriodEnd = source.PeriodEnd,
                PaymentStatus = source.Status,
                OrganizationId = source.OrganizationId,
                SubmittedDate = source.SubmittedDate,
                ExpectedDeliveryDate = source.ExpectedDeliveryDate,
                BillableHours = source.BillableHours,
                InternalHours = source.InternalHours,
                EquityHours = source.EquityHours,
                PrimaryTransactionId = source.PrimaryTransactionId,
                SecondaryTransactionId = source.SecondaryTransactionId,
                ExpenseDetail = source.ExpenseDetail,
                DeductionsDetail = source.DeductionsDetail,
                ContractorPayment = source.ContractorPayment,
                W2Payment = source.W2Payment,
                OfficerPayment = source.OfficierPayment,
                EncryptedGross = source.Gross,
                EncryptedNet = source.Net,
                EncryptedExpenses = source.Expenses,
                EncryptedPrimaryDeposit = source.PrimaryDeposit,
                EncryptedEstimatedTaxWithholding = source.EstimatedDeposit,
                EncryptedEarnedEquity = source.EarnedEquity,

                EncryptedSecondaryDeposit = default,
            };
        }
    }
}
