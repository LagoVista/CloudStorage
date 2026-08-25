using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class AccountLedgerEntry<TRecord>
        where TRecord : IAccountLedgerRecord
    {
        public AccountLedgerEntry(TRecord record, decimal balance, string integrityHash)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Balance = balance;
            IntegrityHash = integrityHash ?? String.Empty;
        }

        public TRecord Record { get; }
        public decimal Balance { get; }
        public string IntegrityHash { get; }
    }

    public sealed class AccountLedgerQuery
    {
        public string OrganizationId { get; set; }
        public string AccountId { get; set; }
        public string TransactionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public StoragePageRequest Page { get; set; } = new StoragePageRequest();
    }
}
