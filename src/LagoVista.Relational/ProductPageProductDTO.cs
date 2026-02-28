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
            modelBuilder.Entity<ProductPageProductDTO>()
            .HasOne(tp => tp.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId);

            modelBuilder.Entity<ProductPageProductDTO>()
            .HasOne(tp => tp.ProductPage)
            .WithMany(pp => pp.ProductPageProducts)
            .HasForeignKey(p => p.ProductPageId);

            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.ProductPageId).HasColumnOrder(2);
            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.ProductId).HasColumnOrder(3);
            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.Discount).HasColumnOrder(4);
            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.Index).HasColumnOrder(5);
            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.UnitQty).HasColumnOrder(6);

            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.Discount).HasDefaultValueSql("0");
            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.Id).HasDefaultValueSql("newid()");
            modelBuilder.Entity<ProductPageProductDTO>().Property(x => x.UnitQty).HasDefaultValueSql("1");
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
