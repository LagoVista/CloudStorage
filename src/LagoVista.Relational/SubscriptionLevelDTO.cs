using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("SubscriptionLevels", Schema = "dbo")]
    public class SubscriptionLevelDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public Guid? ProductId { get; set; }

        public decimal IncludedWorkUnits { get; set; }

        public int? WorkUnitResetCycleTypeId { get; set; }

        public bool AllowsOverage { get; set; }

        public bool IsActive { get; set; } = true;

        public ProductDTO Product { get; set; }

        public RecurringCycleTypeDTO WorkUnitResetCycleType { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<SubscriptionLevelDTO>();

            // Relationships
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkUnitResetCycleType).WithMany().HasForeignKey(x => x.WorkUnitResetCycleTypeId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique().HasDatabaseName("UX_SubscriptionLevels_Key");

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.Key).HasColumnOrder(2);
            entity.Property(x => x.Name).HasColumnOrder(3);
            entity.Property(x => x.Description).HasColumnOrder(4);
            entity.Property(x => x.ProductId).HasColumnOrder(5);
            entity.Property(x => x.IncludedWorkUnits).HasColumnOrder(6);
            entity.Property(x => x.WorkUnitResetCycleTypeId).HasColumnOrder(7);
            entity.Property(x => x.AllowsOverage).HasColumnOrder(8);
            entity.Property(x => x.IsActive).HasColumnOrder(9);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.IncludedWorkUnits).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.WorkUnitResetCycleTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.AllowsOverage).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_SubscriptionLevels_IncludedWorkUnits",
                    $"{nameof(SubscriptionLevelDTO.IncludedWorkUnits)} >= 0");
            });
        }
    }
}
