using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("AccountTransaction",Schema ="dbo")]
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

        [MapFrom("LastUpdatedDate")]
        [Required]
        public DateTime LastUpdateDate { get; set; }


        [MapTo("CreatedBy")]
        [NotMapped]
        public AppUserDTO CreatedByUser { get; set; }

        [MapTo("LastUpdatedBy")]
        [NotMapped]
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
        [NotMapped]
        public VendorDTO Vendor { get; set; }


        [IgnoreOnMapTo]
        public AccountDto Account { get; set; }

        [IgnoreOnMapTo]
        public AccountTransactionCategoryDto Category { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.Account)
                .WithMany(ps => ps.Transactions)
                .HasForeignKey(ps => ps.AccountId); 

            modelBuilder.Entity<AccountTransactionDto>()
              .HasOne(ps => ps.Vendor)
              .WithMany()
              .HasForeignKey(ps => ps.VendorId);

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.Category)
                .WithMany()
                .HasForeignKey(ps => ps.TransactionCategoryId);

            modelBuilder.Entity<AccountTransactionDto>()
                 .HasOne(ps => ps.CreatedByUser)
                 .WithMany()
                 .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.LastUpdatedById);

            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.AccountId).HasColumnOrder(2);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.TransactionDate).HasColumnOrder(3);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.EncryptedAmount).HasColumnOrder(4);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.IsReconciled).HasColumnOrder(5);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.TransactionCategoryId).HasColumnOrder(6);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.Name).HasColumnOrder(7);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.Description).HasColumnOrder(8);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.Tag).HasColumnOrder(9);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.OriginalHash).HasColumnOrder(10);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.CreatedById).HasColumnOrder(11);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.LastUpdatedById).HasColumnOrder(12);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.CreationDate).HasColumnOrder(13);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.LastUpdateDate).HasColumnOrder(14);
            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.VendorId).HasColumnOrder(15);

            modelBuilder.Entity<AccountTransactionDto>().Property(x => x.IsReconciled).HasDefaultValueSql("0");

            modelBuilder.Entity<AccountTransactionDto>().HasKey(x => new { x.Id });

        }
    }
}