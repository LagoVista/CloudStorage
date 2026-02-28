using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    [Table("TransactionStaging", Schema = "dbo")]
    public class TransactionStagingDto
    {
        public Guid Id { get; set; }

        [Required]
        public string ItemId { get; set; }

        public Guid AccountId { get; set; }
        
        [Required]
        public string Name { get; set; }

        public string MerchantName { get; set; }

        public string OriginalDescription { get; set; }

        public string PendingTransactionId { get; set; }

        public string PlaidAccountId { get; set; }

        public string PlaidTransactionId { get; set; }

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

        public AccountDto Account { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionStagingDto>()
                .HasOne(ps => ps.Account)
                .WithMany()
                .HasForeignKey(ps => ps.AccountId);

            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.ItemId).HasColumnOrder(2);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.AccountId).HasColumnOrder(3);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.Name).HasColumnOrder(4);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.MerchantName).HasColumnOrder(5);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.OriginalDescription).HasColumnOrder(6);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.PendingTransactionId).HasColumnOrder(7);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.PlaidAccountId).HasColumnOrder(8);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.PlaidTransactionId).HasColumnOrder(9);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.TransactionType).HasColumnOrder(10);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.AuthorizationDate).HasColumnOrder(11);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.EncryptedAmount).HasColumnOrder(12);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.IsoCurrencyCode).HasColumnOrder(13);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.UnofficialCurrencyCode).HasColumnOrder(14);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.Categories).HasColumnOrder(15);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.CheckNumber).HasColumnOrder(16);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.SuggestedCategory).HasColumnOrder(17);
            modelBuilder.Entity<TransactionStagingDto>().Property(x => x.MerchantEntryId).HasColumnOrder(18);
        }
    }
}
