using System;

namespace MarchDataMigration.Generated.Subscription
{
    // generated: source-side 1:1 shape
    public sealed class SourceSubscriptionRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string OrgId { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string CustomerId { get; set; }
        public string PaymentToken { get; set; }
        public DateTime? PaymentTokenDate { get; set; }
        public DateTime? PaymentTokenExpires { get; set; }
        public string PaymentTokenStatus { get; set; }
        public string Icon { get; set; }
    }
}
