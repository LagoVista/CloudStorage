using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace LagoVista.Relational
{
    [Table("ProductPage", Schema = "dbo")]
    public class ProductPageDTO : DbModelBase
    {
        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        public bool IsPublic { get; set; }

        [Required]
        public string Icon { get; set; } = "icon-pz-product-2";

        [NotMapped]
        [IgnoreOnMapTo]
        public List<ProductPageProductDTO> ProductPageProducts { get; set; } = new List<ProductPageProductDTO>();

        [Required]
        public string ShortSummaryHTML { get; set; }

        [Required]
        public string PageTitle { get; set; }


        public string HeroTitle { get; set; }
        public string HeroTagLine1 { get; set; }
        public string HeroTagLine2 { get; set; }

        [Required]
        public string DescriptionHtml { get; set; }

        [Required]
        public string VideoUrl { get; set; }

        public string HeroImageResourceId { get; set; }

        public string HeroImageResourceName { get; set; }

        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }

        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }
        public string TopLeftMenuId { get; set; }
        public string TopLeftMenuName { get; set; }

        public string TopRightMenuId { get; set; }
        public string TopRightMenuName { get; set; }


        public string ColorPaletteId { get; set; }
        public string ColorPaletteName { get; set; }

        public string ProductPageLayoutId { get; set; }
        public string ProductPageLayoutName { get; set; }

        public string BottomMenuId { get; set; }
        public string BottomMenuName { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ProductPageDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.ProductPageProducts).WithOne().HasForeignKey(x => x.ProductPageId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.Name).HasColumnOrder(3);
            entity.Property(x => x.Key).HasColumnOrder(4);
            entity.Property(x => x.Icon).HasColumnOrder(5);
            entity.Property(x => x.PageTitle).HasColumnOrder(6);
            entity.Property(x => x.ShortSummaryHTML).HasColumnOrder(7);
            entity.Property(x => x.ThumbnailImageResourceId).HasColumnOrder(8);
            entity.Property(x => x.ThumbnailImageResourceName).HasColumnOrder(9);
            entity.Property(x => x.ImageResourceId).HasColumnOrder(10);
            entity.Property(x => x.ImageResourceName).HasColumnOrder(11);
            entity.Property(x => x.HeroImageResourceId).HasColumnOrder(12);
            entity.Property(x => x.HeroImageResourceName).HasColumnOrder(13);
            entity.Property(x => x.HeroTitle).HasColumnOrder(14);
            entity.Property(x => x.HeroTagLine1).HasColumnOrder(15);
            entity.Property(x => x.HeroTagLine2).HasColumnOrder(16);
            entity.Property(x => x.TopLeftMenuId).HasColumnOrder(17);
            entity.Property(x => x.TopLeftMenuName).HasColumnOrder(18);
            entity.Property(x => x.TopRightMenuId).HasColumnOrder(19);
            entity.Property(x => x.TopRightMenuName).HasColumnOrder(20);
            entity.Property(x => x.BottomMenuId).HasColumnOrder(21);
            entity.Property(x => x.BottomMenuName).HasColumnOrder(22);
            entity.Property(x => x.ColorPaletteId).HasColumnOrder(23);
            entity.Property(x => x.ColorPaletteName).HasColumnOrder(24);
            entity.Property(x => x.ProductPageLayoutId).HasColumnOrder(25);
            entity.Property(x => x.ProductPageLayoutName).HasColumnOrder(26);
            entity.Property(x => x.CreatedById).HasColumnOrder(27);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(28);
            entity.Property(x => x.CreationDate).HasColumnOrder(29);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(30);
            entity.Property(x => x.IsPublic).HasColumnOrder(31);
            entity.Property(x => x.DescriptionHtml).HasColumnOrder(32);
            entity.Property(x => x.VideoUrl).HasColumnOrder(33);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.IconStorage(provider));
            entity.Property(x => x.PageTitle).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.ShortSummaryHTML).HasColumnType(StandardDBTypes.HtmlStorage(provider));
            entity.Property(x => x.ThumbnailImageResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ThumbnailImageResourceName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.ImageResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ImageResourceName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.HeroImageResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.HeroImageResourceName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.HeroTitle).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.HeroTagLine1).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.HeroTagLine2).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.TopLeftMenuId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.TopLeftMenuName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.TopRightMenuId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.TopRightMenuName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.BottomMenuId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.BottomMenuName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.ColorPaletteId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ColorPaletteName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.ProductPageLayoutId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ProductPageLayoutName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.IsPublic).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.DescriptionHtml).HasColumnType(StandardDBTypes.HtmlStorage(provider));
            entity.Property(x => x.VideoUrl).HasColumnType(StandardDBTypes.UrlStorage(provider));
        }
    }
}