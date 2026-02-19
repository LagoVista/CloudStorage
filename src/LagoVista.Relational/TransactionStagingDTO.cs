using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class TransactionStagingDto
    {
        public Guid Id { get; set; }

        public string ItemId { get; set; }

        public Guid AccountId { get; set; }

        public string Name { get; set; }

        public string MerchantName { get; set; }

        public string OriginalDescription { get; set; }

        public string PendingTransactionId { get; set; }

        public string PlaidAccountId { get; set; }

        public string PlaidTransactionId { get; set; }

        public string TransactionType { get; set; }

        public DateTime AuthorizationDate { get; set; }

        public string EncryptedAmount { get; set; }

        public string IsoCurrencyCode { get; set; }

        public string UnofficialCurrencyCode { get; set; }

        public string Categories { get; set; }

        public string CheckNumber { get; set; }

        public string SuggestedCategory { get; set; }

        public string MerchantEntryId { get; set; }

        public AccountDto Account { get; set; }

    }
}
