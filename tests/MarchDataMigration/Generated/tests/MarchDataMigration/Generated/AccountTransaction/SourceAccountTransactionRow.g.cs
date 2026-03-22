using System;

namespace MarchDataMigration.Generated.AccountTransaction
{
    // generated: source-side 1:1 shape
    public sealed class SourceAccountTransactionRow
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string EncryptedAmount { get; set; }
        public bool IsReconciled { get; set; }
        public Guid TransactionCategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public string OriginalHash { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public Guid? VendorId { get; set; }
    }
}
