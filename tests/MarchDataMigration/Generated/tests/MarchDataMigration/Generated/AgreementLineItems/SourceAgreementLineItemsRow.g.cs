using System;

namespace MarchDataMigration.Generated.AgreementLineItems
{
    // generated: source-side 1:1 shape
    public sealed class SourceAgreementLineItemsRow
    {
        public Guid Id { get; set; }
        public Guid AgreementId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Extended { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Quantity { get; set; }
        public int UnitTypeId { get; set; }
        public bool IsRecurring { get; set; }
        public int? RecurringCycleTypeId { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public DateTime? LastBilledDate { get; set; }
        public bool Taxable { get; set; }
        public decimal Shipping { get; set; }
    }
}
