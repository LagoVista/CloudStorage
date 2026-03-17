using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{

    [ModernKeyId("user-{id}", IdPath = "UserId")]
    [Table("ExpenseReimbursed", Schema = "dbo")]
    [EncryptionKey("UserId={id}", IdProperty = "UserId")]
    public class ExpenseReimbursementDTO
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public string OrganizationId { get; set; }
        [Required]
        public Guid PaymentId { get; set; }
        [Required]
        public Guid ExpenseId { get; set; }
        [Required]
        public Guid ExpenseCategoryId { get; set; }
        [Required]
        public DateOnly ExpenseDate { get; set; }
        [Required]
        public string EncryptedSubmittedAmount { get; set; }
        [Required]
        public string EncryptedReimbursedAmount { get; set; }
        [Required]
        public string Description { get; set; }


        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }

        [IgnoreOnMapTo]
        public ExpenseDTO Expense { get; set; }

        [IgnoreOnMapTo]
        public ExpenseCategoryDTO ExpenseCategory { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }

        [IgnoreOnMapTo]
        public PaymentDTO Payment { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ExpenseReimbursementDTO>();

            // Relationships
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.Expense).WithMany().HasForeignKey(x => x.ExpenseId);
            entity.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId);
            entity.HasOne(x => x.Payment).WithMany(x => x.ExpenseReimbursements).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.UserId).HasColumnOrder(3);
            entity.Property(x => x.PaymentId).HasColumnOrder(4);
            entity.Property(x => x.ExpenseId).HasColumnOrder(5);
            entity.Property(x => x.ExpenseCategoryId).HasColumnOrder(6);
            entity.Property(x => x.ExpenseDate).HasColumnOrder(7);
            entity.Property(x => x.Description).HasColumnOrder(8);
            entity.Property(x => x.EncryptedSubmittedAmount).HasColumnOrder(9);
            entity.Property(x => x.EncryptedReimbursedAmount).HasColumnOrder(10);


            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.PaymentId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ExpenseId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ExpenseCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ExpenseDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.EncryptedSubmittedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedReimbursedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));

        }

    }
}
