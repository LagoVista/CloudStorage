using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public sealed class AzureTableStorageConnectionSettings 
    {
        public AzureTableStorageConnectionSettings(string accountId, string accountKey)
        {
            if (String.IsNullOrWhiteSpace(accountId)) throw new ArgumentNullException(nameof(accountId));
            if (String.IsNullOrWhiteSpace(accountKey)) throw new ArgumentNullException(nameof(accountKey));

            AccountId = accountId;
            AccountKey = accountKey;
        }

        public string AccountId { get; }
        public string AccountKey { get; }
    }
}
