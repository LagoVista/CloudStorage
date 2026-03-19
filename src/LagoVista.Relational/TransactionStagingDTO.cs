using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;

namespace LagoVista.Relational
{
    [ModernKeyId("external-account-{id}", IdPath = nameof(OrganizationId))]
    [EncryptionKey("external-account-{id}", IdProperty = nameof(OrganizationId), CreateIfMissing = false)]
    [Table("TransactionStaging", Schema = "dbo")]
    public class TransactionStagingDto
    {
        public Guid Id { get; set; }

        [Required]
        public string OrganizationId { get; set; }

        [Required]
        public string IdempotencyHash { get; set; }

        public Guid? AccountId { get; set; }
        public Guid? CreditCardId { get; set; }

        [Required]
        public string TransactionDescription { get; set; }

        public string MerchantName { get; set; }
        public string CheckNumber { get; set; }

        [Required]
        public string PostedDate { get; set; }

        public string ChangeType { get; set; }

        [Required]
        public string EncryptedAmount { get; set; }

        public Guid? SuggestedAccountTransactionCategoryId { get; set; }
        public Guid? SuggestedExpenseCategoryId { get; set; }
        public Guid? SuggestedVendorId { get; set; }

        public double? SuggestedConfidence { get; set; }

        public string PendingVectorRecordId { get; set; }

        public DateTime CreationDate { get; set; }

        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }

        [IgnoreOnMapTo]
        public AccountDto Account { get; set; }

        [IgnoreOnMapTo]
        public CreditCardDTO CreditCard { get; set; }

        [IgnoreOnMapTo]
        public AccountTransactionCategoryDto AccountTransactionCategory { get; set; }

        [IgnoreOnMapTo]
        public ExpenseCategoryDTO ExpenseCategory { get; set; }

        [IgnoreOnMapTo]
        public VendorDTO Vendor { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<TransactionStagingDto>();

            entity.ToTable("TransactionStaging", "dbo");

            entity.HasKey(x => x.Id);

        //    entity.HasOne(x => x.Account).WithMany(x => x.StagedTransactions).HasForeignKey(x => x.AccountId);
        //    entity.HasOne(x => x.CreditCard).WithMany(x => x.StagedTransactions).HasForeignKey(x => x.CreditCardId);

            //entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            //entity.HasOne(x => x.AccountTransactionCategory).WithMany().HasForeignKey(x => x.SuggestedAccountTransactionCategoryId);
            //entity.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.SuggestedExpenseCategoryId);
            //entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.SuggestedVendorId);

            entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyHash }).IsUnique();

            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.IdempotencyHash).HasColumnOrder(3);
            entity.Property(x => x.AccountId).HasColumnOrder(4);
            entity.Property(x => x.CreditCardId).HasColumnOrder(5);
            entity.Property(x => x.TransactionDescription).HasColumnOrder(6);
            entity.Property(x => x.MerchantName).HasColumnOrder(7);
            entity.Property(x => x.CheckNumber).HasColumnOrder(8);
            entity.Property(x => x.PostedDate).HasColumnOrder(9);
            entity.Property(x => x.ChangeType).HasColumnOrder(10);
            entity.Property(x => x.EncryptedAmount).HasColumnOrder(11);
            entity.Property(x => x.SuggestedAccountTransactionCategoryId).HasColumnOrder(12);
            entity.Property(x => x.SuggestedExpenseCategoryId).HasColumnOrder(13);
            entity.Property(x => x.SuggestedVendorId).HasColumnOrder(14);
            entity.Property(x => x.SuggestedConfidence).HasColumnOrder(15);
            entity.Property(x => x.PendingVectorRecordId).HasColumnOrder(16);
            entity.Property(x => x.CreationDate).HasColumnOrder(17);

            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.IdempotencyHash).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.AccountId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreditCardId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.TransactionDescription).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.MerchantName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.CheckNumber).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.PostedDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.ChangeType).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.EncryptedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.SuggestedAccountTransactionCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.SuggestedExpenseCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.SuggestedVendorId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.SuggestedConfidence).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.PendingVectorRecordId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));

            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.IdempotencyHash).IsRequired();
            entity.Property(x => x.TransactionDescription).IsRequired();
            entity.Property(x => x.PostedDate).IsRequired();
            entity.Property(x => x.EncryptedAmount).IsRequired();
            entity.Property(x => x.ChangeType).IsRequired();
            entity.Property(x => x.CreationDate).IsRequired();
        }
    }
}