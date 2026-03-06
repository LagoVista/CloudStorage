using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LagoVista.Relational
{
    [Table("ProductPage_Product",Schema ="dbo")]
    public class ProductPageProductDTO
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid ProductPageId { get; set; }
        public decimal Discount { get; set; }
        public int Index { get; set; }
        public int UnitQty { get; set; }



        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }

        [IgnoreOnMapTo]
        public ProductPageDTO ProductPage { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ProductPageProductDTO>();

            // Relationships
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            entity.HasOne(x => x.ProductPage).WithMany(x => x.ProductPageProducts).HasForeignKey(x => x.ProductPageId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.ProductPageId).HasColumnOrder(2);
            entity.Property(x => x.ProductId).HasColumnOrder(3);
            entity.Property(x => x.Discount).HasColumnOrder(4);
            entity.Property(x => x.Index).HasColumnOrder(5);
            entity.Property(x => x.UnitQty).HasColumnOrder(6);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductPageId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Discount).HasColumnType(StandardDBTypes.MoneyStorage(provider));
            entity.Property(x => x.Index).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.UnitQty).HasColumnType(StandardDBTypes.IntStorage(provider));
        }
    }

    public class ProductPageProductViewDTO
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid ProductPageId { get; set; }

        public decimal Discount { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public string ShortSummaryHTML { get; set; }
        public string DetailsHTML { get; set; }
        public string Description { get; set; }
        public string ImageResourceId { get; set; }
        public string ThumbnailImageResourceId { get; set; }
        public string RemoteResourceId { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitType { get; set; }
        public int Index { get; set; }
        public int UnitQty { get; set; }
        public int Units { get; set; }
        public decimal Extended { get; set; }
        public int UnitTypeId { get; set; }
        public Guid ProductCategoryId { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            
        }
    }
}
