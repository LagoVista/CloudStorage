using System;

namespace MarchDataMigration.Generated.Account
{
    // generated: source-side 1:1 shape
    public sealed class SourceAccountRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RoutingNumber { get; set; }
        public string AccountNumber { get; set; }
        public string Institution { get; set; }
        public bool IsLiability { get; set; }
        public string EncryptedBalance { get; set; }
        public string Description { get; set; }
        public string OrganizationId { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public bool IsActive { get; set; }
        public bool TransactionJournalOnly { get; set; }
    }
}
