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
            modelBuilder.Entity<ProductCategoryDTO>()
           .HasOne(ps => ps.Organization)
           .WithMany()
           .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<ProductCategoryDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductCategoryDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CreatedById).HasColumnOrder(3);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(4);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CreationDate).HasColumnOrder(5);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(6);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Name).HasColumnOrder(7);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Key).HasColumnOrder(8);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Description).HasColumnOrder(9);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.IsPublic).HasColumnOrder(10);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Icon).HasColumnOrder(11);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ThumbnailImageResourceId).HasColumnOrder(12);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ThumbnailImageResourceName).HasColumnOrder(13);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ImageResourceId).HasColumnOrder(14);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ImageResourceName).HasColumnOrder(15);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ShortSummaryHTML).HasColumnOrder(16);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CategoryTypeId).HasColumnOrder(17);
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CategoryTypeName).HasColumnOrder(18);

            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CategoryTypeId).HasDefaultValueSql("'software'");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CategoryTypeName).HasDefaultValueSql("'Software'");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CreationDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Id).HasDefaultValueSql("newid()");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.LastUpdateDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ShortSummaryHTML).HasDefaultValueSql("''");

            modelBuilder.Entity<ProductCategoryDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CategoryTypeId).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CategoryTypeName).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CreatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.CreationDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Description).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Icon).HasColumnType("varchar(50)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Id).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ImageResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ImageResourceName).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.IsPublic).HasColumnType("bit");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Key).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.LastUpdateDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.LastUpdatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.Name).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.OrganizationId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ShortSummaryHTML).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ThumbnailImageResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductCategoryDTO>().Property(x => x.ThumbnailImageResourceName).HasColumnType("varchar(128)");
        }
    }
}
