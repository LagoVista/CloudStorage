using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("budgetitem-{id}", IdPath = "OrganizationId", CreateIfMissing = true)]
    [Table("BudgetItems", Schema = "dbo")]
    [EncryptionKey("BUDGET_ITEM_KEY_{id}", IdProperty = nameof(OrganizationId), CreateIfMissing = false)]
    public class BudgetItemDTO : DbModelBase
    {

        public int Year { get; set; }

        public int Month { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Icon { get; set; }
        [Required]
        public string Description { get; set; }

        public Guid? AccountTransactionCategoryId { get; set; }

        public Guid? ExpenseCategoryId { get; set; }

        public Guid? WorkRoleId { get; set; }

        [Required]
        public string EncryptedAllocated { get; set; }
        [Required]
        public string EncryptedActual { get; set; }

        [NotMapped]
        public AccountTransactionCategoryDto AccountTransactionCategory { get; set; }

        [NotMapped]
        public ExpenseCategoryDTO ExpenseCategory { get; set; }

        [NotMapped]
        public WorkRoleDTO WorkRole { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<BudgetItemDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AccountTransactionCategory).WithMany().HasForeignKey(x => x.AccountTransactionCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkRole).WithMany().HasForeignKey(x => x.WorkRoleId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.Name).HasColumnOrder(2);
            entity.Property(x => x.Icon).HasColumnOrder(3);
            entity.Property(x => x.Year).HasColumnOrder(4);
            entity.Property(x => x.Month).HasColumnOrder(5);
            entity.Property(x => x.OrganizationId).HasColumnOrder(6);
            entity.Property(x => x.AccountTransactionCategoryId).HasColumnOrder(7);
            entity.Property(x => x.ExpenseCategoryId).HasColumnOrder(8);
            entity.Property(x => x.WorkRoleId).HasColumnOrder(9);
            entity.Property(x => x.EncryptedAllocated).HasColumnOrder(10);
            entity.Property(x => x.EncryptedActual).HasColumnOrder(11);
            entity.Property(x => x.CreatedById).HasColumnOrder(12);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(13);
            entity.Property(x => x.CreationDate).HasColumnOrder(14);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(15);
            entity.Property(x => x.Description).HasColumnOrder(16);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.IconStorage(provider));
            entity.Property(x => x.Year).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.Month).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.AccountTransactionCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ExpenseCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.WorkRoleId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.EncryptedAllocated).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedActual).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
        }
    }
}
