

using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("account-{id}",IdPath ="Id")]
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
        public string AccountCode { get; set; }

        [Required]
        public string AccountType { get; set; }

        [Required]
        public string AccountSubtype { get; set; }

        public bool AllowPosting { get; set; }
        public bool IsSystemAccount { get; set; }

        public bool IsContraAccount { get; set; }

        public Guid? ParentAccountId { get; set; }

        [IgnoreOnMapTo]
        public List<TransactionStagingDto> StagedTransactions { get; set; }

        [IgnoreOnMapTo()]
        public List<AccountTransactionDto> Transactions { get; set; }

        [IgnoreOnMapTo]
        public AccountDto ParentAccount { get; set; }


        [IgnoreOnMapTo()]
        public long Version { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<AccountDto>();

            // Relationships
            entity.HasMany(x => x.Transactions).WithOne(x => x.Account).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ParentAccount).WithMany().HasForeignKey(x => x.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.StagedTransactions).WithOne(x => x.Account).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);

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
            entity.Property(x => x.TransactionJournalOnly).HasColumnOrder(15);
            entity.Property(x => x.EncryptedOnlineBalance).HasColumnOrder(16);
            entity.Property(x => x.Version).HasColumnOrder(17);
            entity.Property(x => x.AccountCode).HasColumnOrder(18);
            entity.Property(x => x.AccountType).HasColumnOrder(19);
            entity.Property(x => x.AccountSubtype).HasColumnOrder(20);
            entity.Property(x => x.AllowPosting).HasColumnOrder(21);
            entity.Property(x => x.IsSystemAccount).HasColumnOrder(22);
            entity.Property(x => x.IsContraAccount).HasColumnOrder(23);
            entity.Property(x => x.ParentAccountId).HasColumnOrder(24);


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
            entity.Property(x => x.TransactionJournalOnly).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.EncryptedOnlineBalance).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Version).HasColumnType(StandardDBTypes.LongStorage(provider));

            entity.Property(x => x.IsSystemAccount).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.IsContraAccount).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.AllowPosting).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.AccountCode).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.AccountType).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.AccountSubtype).HasColumnType(StandardDBTypes.CategoryStorage(provider));

            entity.Property(x => x.ParentAccountId).HasColumnType(StandardDBTypes.UuidStorage(provider));
        }
    }
}
