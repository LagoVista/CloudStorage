using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Product", Schema = "dbo")]
    public class ProductDTO: IEntityHeaderFactory
    {
        [Key]
        public Guid Id { get; set; }


        [MapFrom("CreatedBy")]
        [Required]
        public string CreatedById { get; set; }


        [MapFrom("LastUpdatedBy")]
        [Required]
        public string LastUpdatedById { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        [Required]
        public DateTime LastUpdatedDate { get; set; }

        [MapTo("CreatedBy")]
        [IgnoreOnMapTo]
        public AppUserDTO CreatedByUser { get; set; }

        [MapTo("LastUpdatedBy")]
        [IgnoreOnMapTo]
        public AppUserDTO LastUpdatedByUser { get; set; }

        [IgnoreOnMapTo]
        public ProductCategoryDTO ProductCategory { get; set; }

        [Required]
        public Guid ProductCategoryId { get; set; }


        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Sku { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal UnitCost { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public int RecurringCycleTypeId { get; set; } = 1;

        [Required]
        public int UnitTypeId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string DetailsHTML { get; set; }

        [Required]
        public string ShortSummaryHTML { get; set; }


        public string RemoteResourceId { get; set; }

        public bool IsTrialResource { get; set; }

        public bool PhysicalProduct { get; set; }


        public bool IsPublic { get; set; } = true;


        public string Icon { get; set; } = "icon-pz-product-1";

        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }

        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }


        [IgnoreOnMapTo]
        public RecurringCycleTypeDTO RecurringCycleType { get; set; }

        [IgnoreOnMapTo]
        public UnitTypeDTO UnitType { get; set; }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), Key, Name);
        }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ProductDTO>();

            // Relationships
            entity.HasOne(x => x.RecurringCycleType).WithMany().HasForeignKey(x => x.RecurringCycleTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UnitType).WithMany().HasForeignKey(x => x.UnitTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProductCategory).WithMany(x => x.Products).HasForeignKey(x => x.ProductCategoryId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.ProductCategoryId).HasColumnOrder(2);
            entity.Property(x => x.CreatedById).HasColumnOrder(3);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(4);
            entity.Property(x => x.CreationDate).HasColumnOrder(5);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(6);
            entity.Property(x => x.Key).HasColumnOrder(7);
            entity.Property(x => x.Name).HasColumnOrder(8);
            entity.Property(x => x.Sku).HasColumnOrder(9);
            entity.Property(x => x.Status).HasColumnOrder(10);
            entity.Property(x => x.UnitCost).HasColumnOrder(11);
            entity.Property(x => x.UnitTypeId).HasColumnOrder(12);
            entity.Property(x => x.Description).HasColumnOrder(13);
            entity.Property(x => x.DetailsHTML).HasColumnOrder(14);
            entity.Property(x => x.RemoteResourceId).HasColumnOrder(15);
            entity.Property(x => x.IsTrialResource).HasColumnOrder(16);
            entity.Property(x => x.Icon).HasColumnOrder(17);
            entity.Property(x => x.ThumbnailImageResourceId).HasColumnOrder(18);
            entity.Property(x => x.ThumbnailImageResourceName).HasColumnOrder(19);
            entity.Property(x => x.ImageResourceId).HasColumnOrder(20);
            entity.Property(x => x.ImageResourceName).HasColumnOrder(21);
            entity.Property(x => x.PhysicalProduct).HasColumnOrder(22);
            entity.Property(x => x.ShortSummaryHTML).HasColumnOrder(23);
            entity.Property(x => x.UnitPrice).HasColumnOrder(24);
            entity.Property(x => x.IsPublic).HasColumnOrder(25);
            entity.Property(x => x.RecurringCycleTypeId).HasColumnOrder(26);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Sku).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.UnitCost).HasColumnType(StandardDBTypes.MoneyStorage(provider));
            entity.Property(x => x.UnitTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.DetailsHTML).HasColumnType(StandardDBTypes.HtmlStorage(provider));
            entity.Property(x => x.RemoteResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.IsTrialResource).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.IconStorage(provider));
            entity.Property(x => x.ThumbnailImageResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ThumbnailImageResourceName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.ImageResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ImageResourceName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.PhysicalProduct).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.ShortSummaryHTML).HasColumnType(StandardDBTypes.HtmlStorage(provider));
            entity.Property(x => x.UnitPrice).HasColumnType(StandardDBTypes.MoneyStorage(provider));
            entity.Property(x => x.IsPublic).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.RecurringCycleTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
        }
    }
}
