using System;

namespace MarchDataMigration.Generated.Payments
{
    // generated: source-side 1:1 shape
    public sealed class SourcePaymentsRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string UserId { get; set; }
        public Guid TimePeriodId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Status { get; set; }
        public string OrganizationId { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public decimal BillableHours { get; set; }
        public decimal InternalHours { get; set; }
        public decimal EquityHours { get; set; }
        public string Gross { get; set; }
        public string Net { get; set; }
        public string Expenses { get; set; }
        public string PrimaryTransactionId { get; set; }
        public string SecondaryTransactionId { get; set; }
        public string PrimaryDeposit { get; set; }
        public string EstimatedDeposit { get; set; }
        public string ExpenseDetail { get; set; }
        public string DeductionsDetail { get; set; }
        public string EarnedEquity { get; set; }
        public bool ContractorPayment { get; set; }
        public bool W2Payment { get; set; }
        public bool OfficierPayment { get; set; }
    }
}
