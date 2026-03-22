using System;

namespace MarchDataMigration.Generated.Account
{
    // generated: target-side 1:1 shape
    public partial class TargetAccountRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string EncryptedRoutingNumber { get; set; }
        public string EncryptedAccountNumber { get; set; }
        public string Institution { get; set; }
        public bool IsLiability { get; set; }
        public string EncryptedBalance { get; set; }
        public string Description { get; set; }
        public string OrganizationId { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool TransactionJournalOnly { get; set; }
        public string EncryptedOnlineBalance { get; set; }
        public long Version { get; set; }
    }
}
