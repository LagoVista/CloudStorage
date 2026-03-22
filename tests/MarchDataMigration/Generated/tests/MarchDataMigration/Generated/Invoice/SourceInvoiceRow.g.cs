using System;

namespace MarchDataMigration.Generated.Invoice
{
    // generated: source-side 1:1 shape
    public sealed class SourceInvoiceRow
    {
        public Guid Id { get; set; }
        public bool IsMaster { get; set; }
        public bool HasChildren { get; set; }
        public Guid? MasterInvoiceId { get; set; }
        public int InvoiceNumber { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string OrgId { get; set; }
        public Guid? CustomerId { get; set; }
        public string Notes { get; set; }
        public DateTime BillingStart { get; set; }
        public DateTime BillingEnd { get; set; }
        public DateTime CreationTimeStamp { get; set; }
        public DateTime DueDate { get; set; }
        public string Total { get; set; }
        public string Discount { get; set; }
        public string Extended { get; set; }
        public string TotalPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public string ClosedTransactionId { get; set; }
        public string Status { get; set; }
        public DateTime StatusDate { get; set; }
        public int FailedAttemptCount { get; set; }
        public Guid? AgreementId { get; set; }
        public string Shipping { get; set; }
        public string Tax { get; set; }
        public string Subtotal { get; set; }
        public decimal TaxPercent { get; set; }
        public string ContactId { get; set; }
        public string AdditionalNotes { get; set; }
        public bool IsLocked { get; set; }
    }
}
