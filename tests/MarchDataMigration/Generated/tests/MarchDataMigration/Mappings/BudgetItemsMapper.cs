using MarchDataMigration.Generated.BudgetItems;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class BudgetItemsMapper
    {
        public static TargetBudgetItemsRow Map(SourceBudgetItemsRow source)
        {
            return new TargetBudgetItemsRow
            {
                Id = source.Id,
                Name = source.Name,
                Icon = source.Icon,
                Year = source.Year,
                Month = source.Month,
                OrganizationId = source.OrganizationId,
                AccountTransactionCategoryId = source.AccountTransactionCategoryId,
                ExpenseCategoryId = source.ExpenseCategoryId,
                WorkRoleId = source.WorkRoleId,
                EncryptedAllocated = source.EncryptedAllocated,
                EncryptedActual = source.EncryptedActual,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                Description = source.Description,
            };
        }
    }
}
