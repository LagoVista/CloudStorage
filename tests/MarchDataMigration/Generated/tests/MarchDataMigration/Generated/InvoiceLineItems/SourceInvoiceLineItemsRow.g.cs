using System;

namespace MarchDataMigration.Generated.InvoiceLineItems
{
    // generated: source-side 1:1 shape
    public sealed class SourceInvoiceLineItemsRow
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid? AgreementId { get; set; }
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public string Units { get; set; }
        public string UnitPrice { get; set; }
        public string Total { get; set; }
        public string Discount { get; set; }
        public string Extended { get; set; }
        public bool? Taxable { get; set; }
        public Guid? ProductId { get; set; }
        public string Shipping { get; set; }
    }
}
