using MarchDataMigration.Generated.ExpenseCategory;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class ExpenseCategoryMapper
    {
        public static TargetExpenseCategoryRow Map(SourceExpenseCategoryRow source)
        {
            return new TargetExpenseCategoryRow
            {
                Id = source.Id,
                CreatedById = source.CreatedById,
                CreationDate = source.CreationDate,
                LastUpdatedById = source.LastUpdatedById,
                LastUpdatedDate = default,
                OrganizationId = source.OrganizationId,
                Key = source.Key,
                Name = source.Name,
                Description = source.Description,
                ReimbursementPercent = source.ReimbursementPercent,
                DeductiblePercent = source.DeductiblePercent,
                IsActive = source.IsActive,
                RequiresApproval = source.RequiresApproval,
                Icon = source.Icon,
                TaxCategory = source.TaxCategory,
            };
        }
    }
}
