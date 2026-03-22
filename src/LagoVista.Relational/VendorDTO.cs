using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Vendor", Schema = "dbo")]
    public class VendorDTO : DbModelBase, IEntityHeaderFactory
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


        public EntityHeader ToEntityHeader() => EntityHeader.Create(Id.ToString(), Key, Name);


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<VendorDTO>();

            // Relationships
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DefaultExpenseCategory).WithMany().HasForeignKey(x => x.DefaultExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DefaultAccountTransactionCategory).WithMany().HasForeignKey(x => x.DefaultAccountTransactionCategoryId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.DefaultExpenseCategoryId).HasColumnOrder(3);
            entity.Property(x => x.Name).HasColumnOrder(4);
            entity.Property(x => x.Key).HasColumnOrder(5);
            entity.Property(x => x.Description).HasColumnOrder(6);
            entity.Property(x => x.MaxAmount).HasColumnOrder(7);
            entity.Property(x => x.PayPeriod).HasColumnOrder(8);
            entity.Property(x => x.Notes).HasColumnOrder(9);
            entity.Property(x => x.Contact).HasColumnOrder(10);
            entity.Property(x => x.Phone).HasColumnOrder(11);
            entity.Property(x => x.Icon).HasColumnOrder(12);
            entity.Property(x => x.Address1).HasColumnOrder(13);
            entity.Property(x => x.Address2).HasColumnOrder(14);
            entity.Property(x => x.City).HasColumnOrder(15);
            entity.Property(x => x.StateOrProvince).HasColumnOrder(16);
            entity.Property(x => x.PostalCode).HasColumnOrder(17);
            entity.Property(x => x.Country).HasColumnOrder(18);
            entity.Property(x => x.CreatedById).HasColumnOrder(19);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(20);
            entity.Property(x => x.CreationDate).HasColumnOrder(21);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(22);
            entity.Property(x => x.IsActive).HasColumnOrder(23);
            entity.Property(x => x.DefaultAccountTransactionCategoryId).HasColumnOrder(24);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.DefaultExpenseCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.MaxAmount).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.PayPeriod).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.Contact).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Phone).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.IconStorage(provider));
            entity.Property(x => x.Address1).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Address2).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.City).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.StateOrProvince).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.PostalCode).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.Country).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.DefaultAccountTransactionCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
        }
    }
}