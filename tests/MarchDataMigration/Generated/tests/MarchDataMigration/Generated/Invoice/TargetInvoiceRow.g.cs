using System;

namespace MarchDataMigration.Generated.Invoice
{
    // generated: target-side 1:1 shape
    public partial class TargetInvoiceRow
    {
        public Guid Id { get; set; }
        public bool IsMaster { get; set; }
        public Guid? MasterInvoiceId { get; set; }
        public bool HasChildren { get; set; }
        public int InvoiceNumber { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string OrganizationId { get; set; }
        public Guid CustomerId { get; set; }
        public string Notes { get; set; }
        public DateTime BillingStart { get; set; }
        public DateTime BillingEnd { get; set; }
        public DateTime ServicesStart { get; set; }
        public DateTime ServicesEnd { get; set; }
        public DateTime CreationTimestamp { get; set; }
        public DateTime DueDate { get; set; }
        public string EncryptedTotal { get; set; }
        public string EncryptedDiscount { get; set; }
        public string EncryptedExtended { get; set; }
        public string EncryptedTotalPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public string ClosedTransactionId { get; set; }
        public string Status { get; set; }
        public DateTime StatusTimestamp { get; set; }
        public int FailedAttemptCount { get; set; }
        public Guid? AgreementId { get; set; }
        public string EncryptedShipping { get; set; }
        public string EncryptedTax { get; set; }
        public string EncryptedSubtotal { get; set; }
        public decimal TaxPercent { get; set; }
        public string ContactId { get; set; }
        public string ContactName { get; set; }
        public string AdditionalNotes { get; set; }
        public bool IsLocked { get; set; }
        public DateTime InvoiceDate { get; set; }
    }
}
