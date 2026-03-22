using System;

namespace MarchDataMigration.Generated.AccountTransactionCategory
{
    // generated: source-side 1:1 shape
    public sealed class SourceAccountTransactionCategoryRow
    {
        public Guid Id { get; set; }
        public string OrganizationId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public bool IsActive { get; set; }
        public string Icon { get; set; }
        public Guid? ExpenseCategoryId { get; set; }
        public string TaxCategory { get; set; }
        public bool TaxReportable { get; set; }
        public bool Passthrough { get; set; }
    }
}
