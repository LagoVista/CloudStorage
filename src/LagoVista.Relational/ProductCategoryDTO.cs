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
    [Table("ProductCategory", Schema = "dbo")]
    public class ProductCategoryDTO : DbModelBase
    {

        public ProductCategoryDTO()
        {
            Icon = "icon-pz-product-2";
            IsPublic = true;
            CategoryTypeId = "-1";
            CategoryTypeName = "-select category type-";
        }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsPublic { get; set; }

        public string Icon { get; set; }


        [Required]
        public string ShortSummaryHTML { get; set; }


        [Required]
        public string CategoryTypeName { get; set; }

        [Required]
        public string CategoryTypeId { get; set; }


        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }

        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }
        public EntityHeader ToEntityHeader() => EntityHeader.Create(this.Id.ToString(), this.Key, this.Name);


        [IgnoreOnMapTo()]
        public List<ProductDTO> Products { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ProductCategoryDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Defaults
            entity.Property(x => x.CategoryTypeId).HasDefaultValueSql(StandardDbDefaults.Text(provider, "software"));
            entity.Property(x => x.CategoryTypeName).HasDefaultValueSql(StandardDbDefaults.Text(provider, "Software"));
            entity.Property(x => x.Id).HasDefaultValueSql(StandardDbDefaults.NewGuid(provider));
            entity.Property(x => x.ShortSummaryHTML).HasDefaultValueSql(StandardDbDefaults.Text(provider, ""));

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.CreatedById).HasColumnOrder(3);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(4);
            entity.Property(x => x.CreationDate).HasColumnOrder(5);
            entity.Property(x => x.LastUpdateDate).HasColumnOrder(6);
            entity.Property(x => x.Name).HasColumnOrder(7);
            entity.Property(x => x.Key).HasColumnOrder(8);
            entity.Property(x => x.Description).HasColumnOrder(9);
            entity.Property(x => x.IsPublic).HasColumnOrder(10);
            entity.Property(x => x.Icon).HasColumnOrder(11);
            entity.Property(x => x.ThumbnailImageResourceId).HasColumnOrder(12);
            entity.Property(x => x.ThumbnailImageResourceName).HasColumnOrder(13);
            entity.Property(x => x.ImageResourceId).HasColumnOrder(14);
            entity.Property(x => x.ImageResourceName).HasColumnOrder(15);
            entity.Property(x => x.ShortSummaryHTML).HasColumnOrder(16);
            entity.Property(x => x.CategoryTypeId).HasColumnOrder(17);
            entity.Property(x => x.CategoryTypeName).HasColumnOrder(18);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdateDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.IsPublic).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.IconStorage(provider));
            entity.Property(x => x.ThumbnailImageResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ThumbnailImageResourceName).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.ImageResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ImageResourceName).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.ShortSummaryHTML).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CategoryTypeId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.CategoryTypeName).HasColumnType(StandardDBTypes.TextShort(provider));
        }
    }
}
