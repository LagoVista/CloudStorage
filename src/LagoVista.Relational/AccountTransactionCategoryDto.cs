using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("AccountTransactionCategory", Schema = "dbo")]
    public class AccountTransactionCategoryDto : DbModelBase
    {
        public Guid? ExpenseCategoryId { get; set; }


        

        [Required]
        public string Type { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Icon { get; set; }
        public bool IsActive { get; set; }

        public string TaxCategory { get; set; }

        public bool TaxReportable { get; set; }

        public bool Passthrough { get; set; }

        public ExpenseCategoryDTO ExpenseCategory { get; set; }


        public EntityHeader ToEntityHeader()
        {
            return new EntityHeader()
            {
                Id = Id.ToString(),
                Text = Name,
            };
        }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountTransactionCategoryDto>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<AccountTransactionCategoryDto>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById);
    
            modelBuilder.Entity<AccountTransactionCategoryDto>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AccountTransactionCategoryDto>()
            .HasOne(ps => ps.ExpenseCategory)
            .WithMany()
            .HasForeignKey(ps => ps.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.Name).HasColumnOrder(3);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.Type).HasColumnOrder(4);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.Description).HasColumnOrder(5);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.CreatedById).HasColumnOrder(6);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.LastUpdatedById).HasColumnOrder(7);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.CreationDate).HasColumnOrder(8);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.LastUpdateDate).HasColumnOrder(9);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.IsActive).HasColumnOrder(10);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.Icon).HasColumnOrder(11);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.ExpenseCategoryId).HasColumnOrder(12);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.TaxCategory).HasColumnOrder(13);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.TaxReportable).HasColumnOrder(14);
            modelBuilder.Entity<AccountTransactionCategoryDto>().Property(x => x.Passthrough).HasColumnOrder(15);
        }
    }
}
