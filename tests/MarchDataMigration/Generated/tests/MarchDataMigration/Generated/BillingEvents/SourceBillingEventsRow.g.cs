using System;

namespace MarchDataMigration.Generated.BillingEvents
{
    // generated: source-side 1:1 shape
    public sealed class SourceBillingEventsRow
    {
        public Guid Id { get; set; }
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid ProductId { get; set; }
        public DateTime StartTimeStamp { get; set; }
        public string StartedByAppUserId { get; set; }
        public DateTime? EndTimeStamp { get; set; }
        public string EndedByAppUserId { get; set; }
        public decimal? HoursBilled { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? Extended { get; set; }
        public int UnitTypeId { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}
