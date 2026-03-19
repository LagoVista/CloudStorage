using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("credit-card-{id}", IdPath = nameof(OrganizationId))]
    [EncryptionKey("credit-card-{id}", IdProperty = nameof(OrganizationId), CreateIfMissing = false)]
    [Table("CreditCard", Schema = "dbo")]
    public class CreditCardDTO : DbModelBase
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public string CardHolderId { get; set; }

        [Required]
        public string Brand { get; set; }

        [Required]
        public string Last4 { get; set; }

        [Required]
        public int ExpiresMonth { get; set; }

        [Required]
        public int ExpiresYear { get; set; }

        [Required]
        public string Responsibility { get; set; }

        public string EncryptedCreditLimit { get; set; }
        public string EncryptedCurrentBalance { get; set; }
        public string EncryptedOnlineBalance { get; set; }

        [IgnoreOnMapTo]
        public List<ExpenseDTO> Expenses { get; set; }

        [IgnoreOnMapTo]
        public List<TransactionStagingDto> StagedTransactions { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO CardHolder { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<CreditCardDTO>();

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CardHolder).WithMany().HasForeignKey(x => x.CardHolderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Expenses).WithOne(x => x.CreditCard).HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.StagedTransactions).WithOne(x => x.CreditCard).HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Restrict);


            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(3);
            entity.Property(x => x.OrganizationId).HasColumnOrder(4);
            entity.Property(x => x.CreationDate).HasColumnOrder(5);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(6);
            entity.Property(x => x.Name).HasColumnOrder(7);
            entity.Property(x => x.IsActive).HasColumnOrder(8);
            entity.Property(x => x.CardHolderId).HasColumnOrder(9);
            entity.Property(x => x.Brand).HasColumnOrder(10);
            entity.Property(x => x.Last4).HasColumnOrder(11);
            entity.Property(x => x.ExpiresMonth).HasColumnOrder(12);
            entity.Property(x => x.ExpiresYear).HasColumnOrder(13);
            entity.Property(x => x.Responsibility).HasColumnOrder(14);
            entity.Property(x => x.EncryptedCreditLimit).HasColumnOrder(15);
            entity.Property(x => x.EncryptedCurrentBalance).HasColumnOrder(16);
            entity.Property(x => x.EncryptedOnlineBalance).HasColumnOrder(17);

            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.CardHolderId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Brand).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.Last4).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.ExpiresMonth).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.ExpiresYear).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.Responsibility).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.EncryptedCreditLimit).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedCurrentBalance).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedOnlineBalance).HasColumnType(StandardDBTypes.EncryptionStorage(provider));

            entity.Property(x => x.CreatedById).IsRequired();
            entity.Property(x => x.LastUpdatedById).IsRequired();
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.CreationDate).IsRequired();
            entity.Property(x => x.LastUpdatedDate).IsRequired();
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CardHolderId).IsRequired();
            entity.Property(x => x.Brand).IsRequired();
            entity.Property(x => x.Last4).IsRequired();
            entity.Property(x => x.ExpiresMonth).IsRequired();
            entity.Property(x => x.ExpiresYear).IsRequired();
            entity.Property(x => x.Responsibility).IsRequired();
        }
    }
}