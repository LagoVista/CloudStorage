using MarchDataMigration.Generated.AccountTransactionCategory;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class AccountTransactionCategoryMapper
    {
        public static TargetAccountTransactionCategoryRow Map(SourceAccountTransactionCategoryRow source)
        {
            return new TargetAccountTransactionCategoryRow
            {
                Id = source.Id,
                OrganizationId = source.OrganizationId,
                Name = source.Name,
                Type = source.Type,
                Description = source.Description,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                IsActive = source.IsActive,
                Icon = source.Icon,
                ExpenseCategoryId = source.ExpenseCategoryId,
                TaxCategory = source.TaxCategory,
                TaxReportable = source.TaxReportable,
                Passthrough = source.Passthrough,
            };
        }
    }
}
