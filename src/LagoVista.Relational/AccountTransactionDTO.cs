using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{

    [ModernKeyId("account-{id}", IdPath = "AccountId")]
    [Table("AccountTransaction", Schema = "dbo")]
    [EncryptionKey("Account-{id}", IdProperty = nameof(AccountId), CreateIfMissing = false)]
    public class AccountTransactionDto
    {
        [Key]
        public Guid Id { get; set; }
        

        [MapFrom("CreatedBy")]
        [Required]
        public string CreatedById { get; set; }

        [MapFrom("LastUpdatedBy")]
        [Required]
        public string LastUpdatedById { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        [Required]
        public DateTime LastUpdatedDate { get; set; }


        [MapTo("CreatedBy")]
        [IgnoreOnMapTo]
        public AppUserDTO CreatedByUser { get; set; }

        [MapTo("LastUpdatedBy")]
        [IgnoreOnMapTo]
        public AppUserDTO LastUpdatedByUser { get; set; }


        [Required]
        public Guid AccountId { get; set; }

        public Guid? VendorId { get; set; }

        [Required]
        public Guid TransactionCategoryId { get; set; }

        public DateOnly TransactionDate { get; set; }


        [Required]
        public string EncryptedAmount { get; set; }
        public bool IsReconciled { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Tag { get; set; }

        [Required]
        public string OriginalHash { get; set; }




        [IgnoreOnMapTo]
        public VendorDTO Vendor { get; set; }


        [IgnoreOnMapTo]
        public AccountDto Account { get; set; }

        [IgnoreOnMapTo]
        public AccountTransactionCategoryDto Category { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<AccountTransactionDto>();

            // Relationships
            entity.HasOne(x => x.Account).WithMany(x => x.Transactions).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.TransactionCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.AccountId).HasColumnOrder(2);
            entity.Property(x => x.TransactionDate).HasColumnOrder(3);
            entity.Property(x => x.EncryptedAmount).HasColumnOrder(4);
            entity.Property(x => x.IsReconciled).HasColumnOrder(5);
            entity.Property(x => x.TransactionCategoryId).HasColumnOrder(6);
            entity.Property(x => x.Name).HasColumnOrder(7);
            entity.Property(x => x.Description).HasColumnOrder(8);
            entity.Property(x => x.Tag).HasColumnOrder(9);
            entity.Property(x => x.OriginalHash).HasColumnOrder(10);
            entity.Property(x => x.CreatedById).HasColumnOrder(11);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(12);
            entity.Property(x => x.CreationDate).HasColumnOrder(13);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(14);
            entity.Property(x => x.VendorId).HasColumnOrder(15);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.AccountId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.TransactionDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.EncryptedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.IsReconciled).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.TransactionCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.Tag).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.OriginalHash).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.VendorId).HasColumnType(StandardDBTypes.UuidStorage(provider));
        }
    }
}