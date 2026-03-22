using System;

namespace MarchDataMigration.Generated.BudgetItems
{
    // generated: source-side 1:1 shape
    public sealed class SourceBudgetItemsRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string OrganizationId { get; set; }
        public Guid? AccountTransactionCategoryId { get; set; }
        public Guid? ExpenseCategoryId { get; set; }
        public Guid? WorkRoleId { get; set; }
        public string EncryptedAllocated { get; set; }
        public string EncryptedActual { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string Description { get; set; }
    }
}
