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
    [Table("Product", Schema ="dbo")]
    public class ProductDTO
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

        [MapTo("LastUpdatedDate")]
        [Required]
        public DateTime LastUpdateDate { get; set; }

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
            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.RecurringCycleType)
                .WithMany()
                .HasForeignKey(p => p.RecurringCycleTypeId);

            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.UnitType)
                .WithMany()
                .HasForeignKey(p => p.UnitTypeId);

            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.ProductCategory)
                .WithMany()
                .HasForeignKey(p => p.ProductCategoryId);

            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.CreatedByUser)
                .WithMany()
                .HasForeignKey(tp => tp.CreatedById);

            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(tp => tp.LastUpdatedById);

            modelBuilder.Entity<ProductDTO>()
                 .HasOne(p => p.ProductCategory)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.ProductCategoryId)
                 .IsRequired();

            modelBuilder.Entity<ProductDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<ProductDTO>().Property(x => x.ProductCategoryId).HasColumnOrder(2);
            modelBuilder.Entity<ProductDTO>().Property(x => x.CreatedById).HasColumnOrder(3);
            modelBuilder.Entity<ProductDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(4);
            modelBuilder.Entity<ProductDTO>().Property(x => x.CreationDate).HasColumnOrder(5);
            modelBuilder.Entity<ProductDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(6);
            modelBuilder.Entity<ProductDTO>().Property(x => x.Key).HasColumnOrder(7);
            modelBuilder.Entity<ProductDTO>().Property(x => x.Name).HasColumnOrder(8);
            modelBuilder.Entity<ProductDTO>().Property(x => x.Sku).HasColumnOrder(9);
            modelBuilder.Entity<ProductDTO>().Property(x => x.Status).HasColumnOrder(10);
            modelBuilder.Entity<ProductDTO>().Property(x => x.UnitCost).HasColumnOrder(11);
            modelBuilder.Entity<ProductDTO>().Property(x => x.UnitTypeId).HasColumnOrder(12);
            modelBuilder.Entity<ProductDTO>().Property(x => x.Description).HasColumnOrder(13);
            modelBuilder.Entity<ProductDTO>().Property(x => x.DetailsHTML).HasColumnOrder(14);
            modelBuilder.Entity<ProductDTO>().Property(x => x.RemoteResourceId).HasColumnOrder(15);
            modelBuilder.Entity<ProductDTO>().Property(x => x.IsTrialResource).HasColumnOrder(16);
            modelBuilder.Entity<ProductDTO>().Property(x => x.Icon).HasColumnOrder(17);
            modelBuilder.Entity<ProductDTO>().Property(x => x.ThumbnailImageResourceId).HasColumnOrder(18);
            modelBuilder.Entity<ProductDTO>().Property(x => x.ThumbnailImageResourceName).HasColumnOrder(19);
            modelBuilder.Entity<ProductDTO>().Property(x => x.ImageResourceId).HasColumnOrder(20);
            modelBuilder.Entity<ProductDTO>().Property(x => x.ImageResourceName).HasColumnOrder(21);
            modelBuilder.Entity<ProductDTO>().Property(x => x.PhysicalProduct).HasColumnOrder(22);
            modelBuilder.Entity<ProductDTO>().Property(x => x.ShortSummaryHTML).HasColumnOrder(23);
            modelBuilder.Entity<ProductDTO>().Property(x => x.UnitPrice).HasColumnOrder(24);
            modelBuilder.Entity<ProductDTO>().Property(x => x.IsPublic).HasColumnOrder(25);
            modelBuilder.Entity<ProductDTO>().Property(x => x.RecurringCycleTypeId).HasColumnOrder(26);


            modelBuilder.Entity<ProductDTO>().Property(x => x.CreationDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Description).HasDefaultValueSql("''");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Icon).HasDefaultValueSql("'icon-pz-product-1'");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Id).HasDefaultValueSql("newid()");
            modelBuilder.Entity<ProductDTO>().Property(x => x.IsPublic).HasDefaultValueSql("1");
            modelBuilder.Entity<ProductDTO>().Property(x => x.IsTrialResource).HasDefaultValueSql("0");
            modelBuilder.Entity<ProductDTO>().Property(x => x.LastUpdateDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<ProductDTO>().Property(x => x.PhysicalProduct).HasDefaultValueSql("0");
            modelBuilder.Entity<ProductDTO>().Property(x => x.RecurringCycleTypeId).HasDefaultValueSql("1");
            modelBuilder.Entity<ProductDTO>().Property(x => x.ShortSummaryHTML).HasDefaultValueSql("''");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Status).HasDefaultValueSql("'Active'");
            modelBuilder.Entity<ProductDTO>().Property(x => x.UnitPrice).HasDefaultValueSql("0");

            modelBuilder.Entity<ProductDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<ProductDTO>().Property(x => x.CreatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.CreationDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Description).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.DetailsHTML).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Icon).HasColumnType("varchar(50)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Id).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<ProductDTO>().Property(x => x.ImageResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.ImageResourceName).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.IsPublic).HasColumnType("bit");
            modelBuilder.Entity<ProductDTO>().Property(x => x.IsTrialResource).HasColumnType("bit");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Key).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.LastUpdateDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.LastUpdatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Name).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.PhysicalProduct).HasColumnType("bit");
            modelBuilder.Entity<ProductDTO>().Property(x => x.ProductCategoryId).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<ProductDTO>().Property(x => x.RecurringCycleTypeId).HasColumnType("int");
            modelBuilder.Entity<ProductDTO>().Property(x => x.RemoteResourceId).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.ShortSummaryHTML).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Sku).HasColumnType("varchar(max)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.Status).HasColumnType("varchar(50)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.ThumbnailImageResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.ThumbnailImageResourceName).HasColumnType("varchar(128)");
            modelBuilder.Entity<ProductDTO>().Property(x => x.UnitCost).HasColumnType("money");
            modelBuilder.Entity<ProductDTO>().Property(x => x.UnitPrice).HasColumnType("money");
            modelBuilder.Entity<ProductDTO>().Property(x => x.UnitTypeId).HasColumnType("int");
        }
    }
}
