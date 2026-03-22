using System;

namespace MarchDataMigration.Generated.InvoiceLineItems
{
    // generated: target-side 1:1 shape
    public partial class TargetInvoiceLineItemsRow
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? AgreementId { get; set; }
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public string Units { get; set; }
        public string EncryptedUnitPrice { get; set; }
        public string EncryptedTotal { get; set; }
        public string EncryptedDiscount { get; set; }
        public string EncryptedExtended { get; set; }
        public bool? Taxable { get; set; }
        public Guid? ProductId { get; set; }
        public string EncryptedShipping { get; set; }
    }
}
