using System;

namespace MarchDataMigration.Generated.Customers
{
    // generated: source-side 1:1 shape
    public sealed class SourceCustomersRow
    {
        public Guid Id { get; set; }
        public string OrganizationId { get; set; }
        public string CustomerName { get; set; }
        public string BillingContactName { get; set; }
        public string BillingContactEmail { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Notes { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
    }
}
