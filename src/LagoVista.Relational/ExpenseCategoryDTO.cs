using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("ExpenseCategory", Schema = "dbo")]
    public class ExpenseCategoryDTO : DbModelBase, IEntityHeaderFactory
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

        public EntityHeader ToEntityHeader() => EntityHeader.Create(Id.ToString(), Key, Name);

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ExpenseCategoryDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.CreationDate).HasColumnOrder(3);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(4);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(5);
            entity.Property(x => x.OrganizationId).HasColumnOrder(6);
            entity.Property(x => x.Key).HasColumnOrder(7);
            entity.Property(x => x.Name).HasColumnOrder(8);
            entity.Property(x => x.Description).HasColumnOrder(9);
            entity.Property(x => x.ReimbursementPercent).HasColumnOrder(10);
            entity.Property(x => x.DeductiblePercent).HasColumnOrder(11);
            entity.Property(x => x.IsActive).HasColumnOrder(12);
            entity.Property(x => x.RequiresApproval).HasColumnOrder(13);
            entity.Property(x => x.Icon).HasColumnOrder(14);
            entity.Property(x => x.TaxCategory).HasColumnOrder(15);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.ReimbursementPercent).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.DeductiblePercent).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.RequiresApproval).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.IconStorage(provider));
            entity.Property(x => x.TaxCategory).HasColumnType(StandardDBTypes.CategoryStorage(provider));
        }
    }
}