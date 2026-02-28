using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("ExpenseCategory", Schema ="dbo")]
    public class ExpenseCategoryDTO : DbModelBase
    {
        public ExpenseCategoryDTO()
        {
            Icon = "icon-fo-grow-dollar";
        }

        [Required]
        public string Key { get; set; }
        public bool IsActive { get; set; }
        public bool RequiresApproval { get; set; }

        [Required]
        public string Icon { get; set; }

        [Required]
        public string Name { get; set; }

        [Required] 
        public string Description { get; set; }
        public decimal ReimbursementPercent { get; set; }
        public decimal DeductiblePercent { get; set; }

        [Required]
        public string TaxCategory { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExpenseCategoryDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<ExpenseCategoryDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<ExpenseCategoryDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.CreatedById).HasColumnOrder(2);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.CreationDate).HasColumnOrder(3);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(4);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(5);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.OrganizationId).HasColumnOrder(6);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.Key).HasColumnOrder(7);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.Name).HasColumnOrder(8);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.Description).HasColumnOrder(9);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.ReimbursementPercent).HasColumnOrder(10);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.DeductiblePercent).HasColumnOrder(11);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.IsActive).HasColumnOrder(12);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.RequiresApproval).HasColumnOrder(13);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.Icon).HasColumnOrder(14);
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.TaxCategory).HasColumnOrder(15);

            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.Description).HasDefaultValueSql("''");
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.Icon).HasDefaultValueSql("'icon-fo-grow-dollar'");
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.IsActive).HasDefaultValueSql("1");
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.RequiresApproval).HasDefaultValueSql("0");
            modelBuilder.Entity<ExpenseCategoryDTO>().Property(x => x.TaxCategory).HasDefaultValueSql("'Other'");

            modelBuilder.Entity<ExpenseCategoryDTO>().HasKey(x => new { x.Id });
        }
    }
}
