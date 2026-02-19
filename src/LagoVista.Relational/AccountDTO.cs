using LagoVista.Core.Attributes;
using LagoVista.Core.Validation;
using System;

namespace LagoVista.Relational
{
    [EncryptionKey("AccountKey-{id}", IdProperty = nameof(AccountDto.Id))]
    public class AccountDto : DbModelBase
    {
        public string Name { get; set; }
        public string OrganizationId { get; set; }
        public string Institution { get; set; }
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }

        [EncryptedField(nameof(AccountDto.EncryptedBalance), SaltProperty = nameof(AccountDto.Id))]
        public string EncryptedBalance { get; set; }

        [EncryptedField(nameof(AccountDto.EncryptedOnlineBalance), SaltProperty = nameof(AccountDto.Id))]
        public string EncryptedOnlineBalance { get; set; }
        public bool IsLiability { get; set; }
        public string Description { get; set; }

        public bool TransactionJournalOnly { get; set; }

        public bool IsActive { get; set; }

        public string ExternalProvider { get; set; }

        public string ExternalAccountId { get; set; }
        public string AccessTokenSecretId { get; set; }

        public string TransactionCursor { get; set; }
        public string SyncStatus { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public string LastError { get; set; }
        public bool LinkActive { get; set; }
        public string ExternalProviderid { get; set; }

        public ValidationResult Validate()
        {
            var result = base.ValidateCommon();
            if (string.IsNullOrEmpty(Name)) result.AddUserError("Name is required.");
            if (string.IsNullOrEmpty(Institution)) result.AddUserError("Institution is required.");
            if (string.IsNullOrEmpty(AccountNumber)) result.AddUserError("Account number is required.");
            if (string.IsNullOrEmpty(RoutingNumber)) result.AddUserError("Routing number is required.");
            return result;
        }
    }
}
