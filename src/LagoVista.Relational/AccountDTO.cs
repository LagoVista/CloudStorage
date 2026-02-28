

using LagoVista.Core.Attributes;
using LagoVista.Core.Validation;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LagoVista.Relational
{
    [EncryptionKey("Account-{id}", IdProperty = nameof(AccountDto.Id), CreateIfMissing = false)]
    public class AccountDto : DbModelBase
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Institution { get; set; }
        [Required]
        public string AccountNumber { get; set; }
        [Required]
        public string RoutingNumber { get; set; }

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
        public string ExternalProviderid { get; set; }

        [IgnoreOnMapTo()]
        public List<AccountTransactionDto> Transactions { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountDto>()
            .HasMany(ps => ps.Transactions)
            .WithOne(ps => ps.Account)
            .HasForeignKey(ps => ps.AccountId);

            modelBuilder.Entity<AccountDto>()
              .HasOne(ps => ps.CreatedByUser)
              .WithMany()
              .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<AccountDto>()
                .HasOne(ps => ps.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.LastUpdatedById);

            modelBuilder.Entity<AccountDto>()
                .HasOne(ps => ps.Organization)
                .WithMany()
                .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<AccountDto>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<AccountDto>().Property(x => x.Name).HasColumnOrder(2);
            modelBuilder.Entity<AccountDto>().Property(x => x.RoutingNumber).HasColumnOrder(3);
            modelBuilder.Entity<AccountDto>().Property(x => x.AccountNumber).HasColumnOrder(4);
            modelBuilder.Entity<AccountDto>().Property(x => x.Institution).HasColumnOrder(5);
            modelBuilder.Entity<AccountDto>().Property(x => x.IsLiability).HasColumnOrder(6);
            modelBuilder.Entity<AccountDto>().Property(x => x.EncryptedBalance).HasColumnOrder(7);
            modelBuilder.Entity<AccountDto>().Property(x => x.Description).HasColumnOrder(8);
            modelBuilder.Entity<AccountDto>().Property(x => x.OrganizationId).HasColumnOrder(9);
            modelBuilder.Entity<AccountDto>().Property(x => x.CreatedById).HasColumnOrder(10);
            modelBuilder.Entity<AccountDto>().Property(x => x.LastUpdatedById).HasColumnOrder(11);
            modelBuilder.Entity<AccountDto>().Property(x => x.CreationDate).HasColumnOrder(12);
            modelBuilder.Entity<AccountDto>().Property(x => x.LastUpdateDate).HasColumnOrder(13);
            modelBuilder.Entity<AccountDto>().Property(x => x.IsActive).HasColumnOrder(14);
            modelBuilder.Entity<AccountDto>().Property(x => x.LinkActive).HasColumnOrder(15);
            modelBuilder.Entity<AccountDto>().Property(x => x.TransactionJournalOnly).HasColumnOrder(16);
            modelBuilder.Entity<AccountDto>().Property(x => x.ExternalProvider).HasColumnOrder(17);
            modelBuilder.Entity<AccountDto>().Property(x => x.ExternalAccountId).HasColumnOrder(18);
            modelBuilder.Entity<AccountDto>().Property(x => x.AccessTokenSecretId).HasColumnOrder(19);
            modelBuilder.Entity<AccountDto>().Property(x => x.TransactionCursor).HasColumnOrder(20);
            modelBuilder.Entity<AccountDto>().Property(x => x.SyncStatus).HasColumnOrder(21);
            modelBuilder.Entity<AccountDto>().Property(x => x.LastSyncAt).HasColumnOrder(22);
            modelBuilder.Entity<AccountDto>().Property(x => x.LastError).HasColumnOrder(23);
            modelBuilder.Entity<AccountDto>().Property(x => x.EncryptedOnlineBalance).HasColumnOrder(24);
            modelBuilder.Entity<AccountDto>().Property(x => x.ExternalProviderid).HasColumnOrder(25);


        }
    }
}
