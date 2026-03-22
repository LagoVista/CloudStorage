using MarchDataMigration.Generated.AccountTransaction;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class AccountTransactionMapper
    {
        public static TargetAccountTransactionRow Map(SourceAccountTransactionRow source)
        {
            return new TargetAccountTransactionRow
            {
                Id = source.Id,
                AccountId = source.AccountId,
                TransactionDate = source.TransactionDate,
                EncryptedAmount = source.EncryptedAmount,
                IsReconciled = source.IsReconciled,
                TransactionCategoryId = source.TransactionCategoryId,
                Name = source.Name,
                Description = source.Description,
                Tag = source.Tag,
                OriginalHash = source.OriginalHash,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                VendorId = source.VendorId,
            };
        }
    }
}
