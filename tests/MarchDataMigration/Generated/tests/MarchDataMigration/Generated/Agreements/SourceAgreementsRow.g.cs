using System;

namespace MarchDataMigration.Generated.Agreements
{
    // generated: source-side 1:1 shape
    public sealed class SourceAgreementsRow
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string OrganizationId { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public int Terms { get; set; }
        public string InvoicePeriod { get; set; }
        public bool Locked { get; set; }
        public bool Internal { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string Status { get; set; }
        public decimal? Hours { get; set; }
        public string EncryptedRate { get; set; }
        public string Notes { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public DateTime? NextInvoiceDate { get; set; }
        public DateTime? LastInvoicedDate { get; set; }
        public string CustomerContactName { get; set; }
        public string CustomerContactId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Tax { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public decimal TaxPercent { get; set; }
    }
}
