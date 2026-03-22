using System;

namespace MarchDataMigration.Generated.BillingEvents
{
    // generated: target-side 1:1 shape
    public partial class TargetBillingEventsRow
    {
        public Guid Id { get; set; }
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid ProductId { get; set; }
        public DateTime StartTimestamp { get; set; }
        public string StartedByAppUserId { get; set; }
        public DateTime? EndTimestamp { get; set; }
        public DateTime? RolloverAt { get; set; }
        public int BillingTimeZoneId { get; set; }
        public DateTime? BillingDate { get; set; }
        public string EndedByAppUserId { get; set; }
        public decimal? HoursBilled { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? Extended { get; set; }
        public int UnitTypeId { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public decimal? UnitPrice { get; set; }
        public long? Tokens { get; set; }
    }
}
