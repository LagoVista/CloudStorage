using System;

namespace MarchDataMigration.Generated.Vendor
{
    // generated: target-side 1:1 shape
    public partial class TargetVendorRow
    {
        public Guid Id { get; set; }
        public string OrganizationId { get; set; }
        public Guid DefaultExpenseCategoryId { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public decimal MaxAmount { get; set; }
        public string PayPeriod { get; set; }
        public string Notes { get; set; }
        public string Contact { get; set; }
        public string Phone { get; set; }
        public string Icon { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public Guid? DefaultAccountTransactionCategoryId { get; set; }
    }
}
