using System;

namespace MarchDataMigration.Generated.ExpenseCategory
{
    // generated: source-side 1:1 shape
    public sealed class SourceExpenseCategoryRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime LastUpdateDate { get; set; }
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
