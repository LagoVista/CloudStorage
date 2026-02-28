using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
            modelBuilder.Entity<ProductPageDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);
            
            modelBuilder.Entity<ProductPageDTO>()
            .HasMany(p => p.ProductPageProducts)
            .WithOne()
            .HasForeignKey(ppp => ppp.ProductPageId);

            modelBuilder.Entity<ProductPageDTO>()
            .HasOne(tp => tp.CreatedByUser)
            .WithMany()
            .HasForeignKey(tp => tp.CreatedById);

            modelBuilder.Entity<ProductPageDTO>()
            .HasOne(tp => tp.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(tp => tp.LastUpdatedById);


            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Name).HasColumnOrder(3);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Key).HasColumnOrder(4);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Icon).HasColumnOrder(5);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.PageTitle).HasColumnOrder(6);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ShortSummaryHTML).HasColumnOrder(7);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ThumbnailImageResourceId).HasColumnOrder(8);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ThumbnailImageResourceName).HasColumnOrder(9);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ImageResourceId).HasColumnOrder(10);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ImageResourceName).HasColumnOrder(11);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroImageResourceId).HasColumnOrder(12);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroImageResourceName).HasColumnOrder(13);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroTitle).HasColumnOrder(14);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroTagLine1).HasColumnOrder(15);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroTagLine2).HasColumnOrder(16);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopLeftMenuId).HasColumnOrder(17);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopLeftMenuName).HasColumnOrder(18);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopRightMenuId).HasColumnOrder(19);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopRightMenuName).HasColumnOrder(20);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.BottomMenuId).HasColumnOrder(21);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.BottomMenuName).HasColumnOrder(22);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ColorPaletteId).HasColumnOrder(23);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ColorPaletteName).HasColumnOrder(24);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ProductPageLayoutId).HasColumnOrder(25);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ProductPageLayoutName).HasColumnOrder(26);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.CreatedById).HasColumnOrder(27);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(28);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.CreationDate).HasColumnOrder(29);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(30);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.IsPublic).HasColumnOrder(31);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.DescriptionHtml).HasColumnOrder(32);
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.VideoUrl).HasColumnOrder(33);

            modelBuilder.Entity<ProductPageDTO>().Property(x => x.CreationDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.DescriptionHtml).HasDefaultValueSql("''");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Id).HasDefaultValueSql("newid()");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.IsPublic).HasDefaultValueSql("0");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.LastUpdateDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.PageTitle).HasDefaultValueSql("'Product Page'");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.VideoUrl).HasDefaultValueSql("''");

            modelBuilder.Entity<ProductPageDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<ProductPageDTO>().Property(x => x.BottomMenuId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.BottomMenuName).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ColorPaletteId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ColorPaletteName).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.CreatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.CreationDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.DescriptionHtml).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroImageResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroImageResourceName).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroTagLine1).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroTagLine2).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.HeroTitle).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Icon).HasColumnType("varchar(50)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Id).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ImageResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ImageResourceName).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.IsPublic).HasColumnType("bit");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Key).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.LastUpdateDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.LastUpdatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.Name).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.OrganizationId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.PageTitle).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ProductPageLayoutId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ProductPageLayoutName).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ShortSummaryHTML).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ThumbnailImageResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.ThumbnailImageResourceName).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopLeftMenuId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopLeftMenuName).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopRightMenuId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.TopRightMenuName).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductPageDTO>().Property(x => x.VideoUrl).HasColumnType("varchar(max)");
        }
    }
}