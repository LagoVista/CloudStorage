using System;

namespace MarchDataMigration.Generated.Subscription
{
    // generated: target-side 1:1 shape
    public partial class TargetSubscriptionRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string OrganizationId { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public Guid? CustomerId { get; set; }
        public string PaymentTokenCustomerId { get; set; }
        public string PaymentTokenSecretId { get; set; }
        public DateTime? PaymentTokenDate { get; set; }
        public DateTime? PaymentTokenExpires { get; set; }
        public string PaymentTokenStatus { get; set; }
        public string Icon { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string PaymentAccountId { get; set; }
        public string PaymentAccountType { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ActiveDate { get; set; }
        public DateTime? InactiveDate { get; set; }
        public DateTime? TrialStartDate { get; set; }
        public DateTime? TrialExpirationDate { get; set; }
        public bool IsTrial { get; set; }
    }
}
