using MarchDataMigration.Generated.Account;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class AccountMapper
    {
        public static TargetAccountRow Map(SourceAccountRow source)
        {
            return new TargetAccountRow
            {
                Id = source.Id,
                Name = source.Name,
                EncryptedRoutingNumber = source.RoutingNumber,
                EncryptedAccountNumber = source.AccountNumber,
                Institution = source.Institution,
                IsLiability = source.IsLiability,
                EncryptedBalance = source.EncryptedBalance,
                Description = source.Description,
                OrganizationId = source.OrganizationId,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.CreationDate,
                IsActive = source.IsActive,
                TransactionJournalOnly = source.TransactionJournalOnly,
                EncryptedOnlineBalance = default,
                Version = 1,
            };
        }
    }
}
