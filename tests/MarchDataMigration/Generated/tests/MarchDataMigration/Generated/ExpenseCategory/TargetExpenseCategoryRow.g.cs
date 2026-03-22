using System;

namespace MarchDataMigration.Generated.ExpenseCategory
{
    // generated: target-side 1:1 shape
    public partial class TargetExpenseCategoryRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string OrganizationId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal ReimbursementPercent { get; set; }
        public decimal DeductiblePercent { get; set; }
        public bool IsActive { get; set; }
        public bool RequiresApproval { get; set; }
        public string Icon { get; set; }
        public string TaxCategory { get; set; }
    }
}
