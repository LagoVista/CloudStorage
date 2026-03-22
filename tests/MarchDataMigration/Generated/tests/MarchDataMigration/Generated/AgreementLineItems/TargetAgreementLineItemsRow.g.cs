using System;

namespace MarchDataMigration.Generated.AgreementLineItems
{
    // generated: target-side 1:1 shape
    public partial class TargetAgreementLineItemsRow
    {
        public Guid Id { get; set; }
        public Guid AgreementId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CustomerId { get; set; }
        public string ProductName { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string EncryptedUnitPrice { get; set; }
        public string EncryptedDiscountPercent { get; set; }
        public string EncryptedExtended { get; set; }
        public string EncryptedSubTotal { get; set; }
        public decimal Quantity { get; set; }
        public int UnitTypeId { get; set; }
        public bool IsRecurring { get; set; }
        public int? RecurringCycleTypeId { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public DateTime? LastBilledDate { get; set; }
        public bool Taxable { get; set; }
        public string EncryptedShipping { get; set; }
    }
}
