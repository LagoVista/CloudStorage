using System;

namespace MarchDataMigration.Generated.Payments
{
    // generated: target-side 1:1 shape
    public partial class TargetPaymentsRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string UserId { get; set; }
        public Guid TimePeriodId { get; set; }
        public Guid PayrollRunId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string PaymentStatus { get; set; }
        public string OrganizationId { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public decimal BillableHours { get; set; }
        public decimal InternalHours { get; set; }
        public decimal EquityHours { get; set; }
        public string EncryptedGross { get; set; }
        public string EncryptedNet { get; set; }
        public string EncryptedExpenses { get; set; }
        public string PrimaryTransactionId { get; set; }
        public string SecondaryTransactionId { get; set; }
        public string EncryptedPrimaryDeposit { get; set; }
        public string EncryptedEstimatedTaxWithholding { get; set; }
        public string ExpenseDetail { get; set; }
        public string DeductionsDetail { get; set; }
        public string EncryptedEarnedEquity { get; set; }
        public bool ContractorPayment { get; set; }
        public bool W2Payment { get; set; }
        public bool OfficerPayment { get; set; }
        public string EncryptedSecondaryDeposit { get; set; }
    }
}
