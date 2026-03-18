using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("exteranl-account-{id}", IdPath = "ItemId")]
    [EncryptionKey("exteranl-account-{id}", IdProperty = nameof(TransactionStagingDto.ProviderAccountId), CreateIfMissing = false)]

    [Table("TransactionStaging", Schema = "dbo")]
    public class TransactionStagingDto
    {
        public Guid Id { get; set; }

        [Required]
        public string ItemId { get; set; }

        public Guid? AccountId { get; set; }
        public string CreditCardId { get; set; }

        [Required]
        public string Name { get; set; }

        public string MerchantName { get; set; }

        public string OriginalDescription { get; set; }

        public string PendingTransactionId { get; set; }

        public string Provider { get; set; }
        public string ProviderId { get; set; }
        public string ProviderAccountId { get; set; }
        public string ExternalTransactionId { get; set; }

        public string TransactionType { get; set; }

        public DateTime AuthorizationDate { get; set; }

        [Required]
        public string EncryptedAmount { get; set; }

        public string IsoCurrencyCode { get; set; }

        public string UnofficialCurrencyCode { get; set; }

        public string Categories { get; set; }

        public string CheckNumber { get; set; }

        public string SuggestedCategory { get; set; }

        public string MerchantEntryId { get; set; }

        [IgnoreOnMapTo()]
        public AccountDto Account { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<TransactionStagingDto>();

            // Relationships
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.ItemId).HasColumnOrder(2);
            entity.Property(x => x.AccountId).HasColumnOrder(3);
            entity.Property(x => x.CreditCardId).HasColumnOrder(3);
            entity.Property(x => x.Name).HasColumnOrder(5);
            entity.Property(x => x.MerchantName).HasColumnOrder(6);
            entity.Property(x => x.OriginalDescription).HasColumnOrder(7);
            entity.Property(x => x.PendingTransactionId).HasColumnOrder(8);
            entity.Property(x => x.Provider).HasColumnOrder(9);
            entity.Property(x => x.ProviderId).HasColumnOrder(10);
            entity.Property(x => x.ProviderAccountId).HasColumnOrder(11);
            entity.Property(x => x.ExternalTransactionId).HasColumnOrder(12);
            entity.Property(x => x.TransactionType).HasColumnOrder(13);
            entity.Property(x => x.AuthorizationDate).HasColumnOrder(14);
            entity.Property(x => x.EncryptedAmount).HasColumnOrder(15);
            entity.Property(x => x.IsoCurrencyCode).HasColumnOrder(16);
            entity.Property(x => x.UnofficialCurrencyCode).HasColumnOrder(17);
            entity.Property(x => x.Categories).HasColumnOrder(18);
            entity.Property(x => x.CheckNumber).HasColumnOrder(19);
            entity.Property(x => x.SuggestedCategory).HasColumnOrder(20);
            entity.Property(x => x.MerchantEntryId).HasColumnOrder(21);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ItemId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.AccountId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreditCardId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.MerchantName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.OriginalDescription).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.PendingTransactionId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Provider).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.ProviderId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.ProviderAccountId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.ExternalTransactionId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.TransactionType).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.AuthorizationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.EncryptedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.IsoCurrencyCode).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.UnofficialCurrencyCode).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.Categories).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CheckNumber).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.SuggestedCategory).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.MerchantEntryId).HasColumnType(StandardDBTypes.TextMedium(provider));
        }
    }
}
