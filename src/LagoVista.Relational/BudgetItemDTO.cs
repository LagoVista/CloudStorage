using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
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

        [NotMapped]
        public AccountTransactionCategoryDto AccountTransactionCategory { get; set; }

        [NotMapped]
        public ExpenseCategoryDTO ExpenseCategory { get; set; }

        [NotMapped]
        public WorkRoleDTO WorkRole { get; set; }


        [Required]
        public string EncryptedAllocated { get; set; }
        [Required]
        public string EncryptedActual { get; set; }
        public static void Configure(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.AccountTransactionCategory)
            .WithMany()
            .HasForeignKey(ps => ps.AccountTransactionCategoryId);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.ExpenseCategory)
            .WithMany()
            .HasForeignKey(ps => ps.ExpenseCategoryId);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.WorkRole)
            .WithMany()
            .HasForeignKey(ps => ps.WorkRoleId);

            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.Name).HasColumnOrder(2);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.Icon).HasColumnOrder(3);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.Year).HasColumnOrder(4);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.Month).HasColumnOrder(5);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.OrganizationId).HasColumnOrder(6);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.AccountTransactionCategoryId).HasColumnOrder(7);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.ExpenseCategoryId).HasColumnOrder(8);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.WorkRoleId).HasColumnOrder(9);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.EncryptedAllocated).HasColumnOrder(10);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.EncryptedActual).HasColumnOrder(11);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.CreatedById).HasColumnOrder(12);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(13);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.CreationDate).HasColumnOrder(14);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(15);
            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.Description).HasColumnOrder(16);

            modelBuilder.Entity<BudgetItemDTO>().Property(x => x.Id).HasDefaultValueSql("newid()");

            modelBuilder.Entity<BudgetItemDTO>().HasKey(x => new { x.Id });
        }
    }
}
