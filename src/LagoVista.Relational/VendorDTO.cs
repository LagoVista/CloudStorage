using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Vendor", Schema = "dbo")]
    public class VendorDTO : DbModelBase
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Key { get; set; }
        [Required]
        public string Contact { get; set; }
        [Required]
        public string Phone { get; set; }

        [Required]
        public string Description { get; set; }


        [Required]
        public string Notes { get; set; }
        [Required]
        public string Icon { get; set; }

        public Guid DefaultExpenseCategoryId { get; set; }
        public Guid? DefaultAccountTransactionCategoryId { get; set; }


        [Required]
        public string Address1 { get; set; }

        [Required] 
        public string Address2 { get; set; }
        [Required]
        public string City { get; set; }

        [Required] 
        public string StateOrProvince { get; set; }

        [Required] 
        public string Country { get; set; }

        [Required] 
        public string PostalCode { get; set; }
        [Required]
        public string PayPeriod { get; set; }
        public decimal MaxAmount { get; set; }

        public bool IsActive { get; set; }

        [IgnoreOnMapTo()]
        public ExpenseCategoryDTO DefaultExpenseCategory { get; set; }


        [IgnoreOnMapTo()]
        public AccountTransactionCategoryDto DefaultAccountTransactionCategory { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<VendorDTO>()
             .HasOne(ps => ps.DefaultExpenseCategory)
             .WithMany()
             .HasForeignKey(ps => ps.DefaultExpenseCategoryId)
             .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.DefaultAccountTransactionCategory)
            .WithMany()
            .HasForeignKey(ps => ps.DefaultAccountTransactionCategoryId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VendorDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<VendorDTO>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<VendorDTO>().Property(x => x.DefaultExpenseCategoryId).HasColumnOrder(3);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Name).HasColumnOrder(4);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Key).HasColumnOrder(5);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Description).HasColumnOrder(6);
            modelBuilder.Entity<VendorDTO>().Property(x => x.MaxAmount).HasColumnOrder(7);
            modelBuilder.Entity<VendorDTO>().Property(x => x.PayPeriod).HasColumnOrder(8);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Notes).HasColumnOrder(9);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Contact).HasColumnOrder(10);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Phone).HasColumnOrder(11);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Icon).HasColumnOrder(12);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Address1).HasColumnOrder(13);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Address2).HasColumnOrder(14);
            modelBuilder.Entity<VendorDTO>().Property(x => x.City).HasColumnOrder(15);
            modelBuilder.Entity<VendorDTO>().Property(x => x.StateOrProvince).HasColumnOrder(16);
            modelBuilder.Entity<VendorDTO>().Property(x => x.PostalCode).HasColumnOrder(17);
            modelBuilder.Entity<VendorDTO>().Property(x => x.Country).HasColumnOrder(18);
            modelBuilder.Entity<VendorDTO>().Property(x => x.CreatedById).HasColumnOrder(19);
            modelBuilder.Entity<VendorDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(20);
            modelBuilder.Entity<VendorDTO>().Property(x => x.CreationDate).HasColumnOrder(21);
            modelBuilder.Entity<VendorDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(22);
            modelBuilder.Entity<VendorDTO>().Property(x => x.IsActive).HasColumnOrder(23);
            modelBuilder.Entity<VendorDTO>().Property(x => x.DefaultAccountTransactionCategoryId).HasColumnOrder(24);
        }
    }
}
