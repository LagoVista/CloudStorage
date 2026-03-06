

using LagoVista.Core.Attributes;
using LagoVista.Core.Validation;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Account", Schema = "dbo")]
    [EncryptionKey("Account-{id}", IdProperty = nameof(AccountDto.Id), CreateIfMissing = false)]
    public class AccountDto : DbModelBase
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Institution { get; set; }
        [Required]
        public string EncryptedAccountNumber { get; set; }
        [Required]
        public string EncryptedRoutingNumber { get; set; }

        [Required]
        public string EncryptedBalance { get; set; }

        public string EncryptedOnlineBalance { get; set; }
        public bool IsLiability { get; set; }
        [Required]
        public string Description { get; set; }

        public bool TransactionJournalOnly { get; set; }

        public bool IsActive { get; set; }

        [Required]
        public string ExternalProvider { get; set; }

        public string ExternalAccountId { get; set; }
        public string AccessTokenSecretId { get; set; }

        public string TransactionCursor { get; set; }
        public string SyncStatus { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public string LastError { get; set; }
        public bool LinkActive { get; set; }
        public string ExternalProviderId { get; set; }

        [IgnoreOnMapTo()]
        public List<AccountTransactionDto> Transactions { get; set; }

        [IgnoreOnMapTo()]
        public long Version { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<AccountDto>();

            // Relationships
            entity.HasMany(x => x.Transactions).WithOne(x => x.Account).HasForeignKey(x => x.AccountId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.Id).ValueGeneratedNever();

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.Name).HasColumnOrder(2);
            entity.Property(x => x.EncryptedRoutingNumber).HasColumnOrder(3);
            entity.Property(x => x.EncryptedAccountNumber).HasColumnOrder(4);
            entity.Property(x => x.Institution).HasColumnOrder(5);
            entity.Property(x => x.IsLiability).HasColumnOrder(6);
            entity.Property(x => x.EncryptedBalance).HasColumnOrder(7);
            entity.Property(x => x.Description).HasColumnOrder(8);
            entity.Property(x => x.OrganizationId).HasColumnOrder(9);
            entity.Property(x => x.CreatedById).HasColumnOrder(10);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(11);
            entity.Property(x => x.CreationDate).HasColumnOrder(12);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(13);
            entity.Property(x => x.IsActive).HasColumnOrder(14);
            entity.Property(x => x.LinkActive).HasColumnOrder(15);
            entity.Property(x => x.TransactionJournalOnly).HasColumnOrder(16);
            entity.Property(x => x.ExternalProvider).HasColumnOrder(17);
            entity.Property(x => x.ExternalAccountId).HasColumnOrder(18);
            entity.Property(x => x.AccessTokenSecretId).HasColumnOrder(19);
            entity.Property(x => x.TransactionCursor).HasColumnOrder(20);
            entity.Property(x => x.SyncStatus).HasColumnOrder(21);
            entity.Property(x => x.LastSyncAt).HasColumnOrder(22);
            entity.Property(x => x.LastError).HasColumnOrder(23);
            entity.Property(x => x.EncryptedOnlineBalance).HasColumnOrder(24);
            entity.Property(x => x.ExternalProviderId).HasColumnOrder(25);
            entity.Property(x => x.Version).HasColumnOrder(26);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.EncryptedRoutingNumber).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedAccountNumber).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Institution).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.IsLiability).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.EncryptedBalance).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.LinkActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.TransactionJournalOnly).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.ExternalProvider).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.ExternalAccountId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.AccessTokenSecretId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.TransactionCursor).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.SyncStatus).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.LastSyncAt).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastError).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.EncryptedOnlineBalance).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.ExternalProviderId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Version).HasColumnType(StandardDBTypes.LongStorage(provider));
        }
    }
}
