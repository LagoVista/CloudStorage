using System;

namespace MarchDataMigration.Generated.Agreements
{
    // generated: target-side 1:1 shape
    public partial class TargetAgreementsRow
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string OrganizationId { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public bool Locked { get; set; }
        public bool Internal { get; set; }
        public string InvoicePeriod { get; set; }
        public int Terms { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string Status { get; set; }
        public decimal? Hours { get; set; }
        public string EncryptedRate { get; set; }
        public string Notes { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public DateTime? LastInvoicedDate { get; set; }
        public DateTime? NextInvoiceDate { get; set; }
        public string CustomerContactId { get; set; }
        public string CustomerContactName { get; set; }
        public string EncryptedSubTotal { get; set; }
        public string EncryptedDiscountPercent { get; set; }
        public string EncryptedTax { get; set; }
        public string EncryptedShipping { get; set; }
        public string EncryptedTotal { get; set; }
        public decimal TaxPercent { get; set; }
    }
}
