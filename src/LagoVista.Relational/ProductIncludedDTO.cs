using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("ProductIncluded", Schema = "dbo")]
    public class ProductIncludedDTO
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid PackageId { get; set; }
        [Required]
        public Guid ProductId { get; set; }
        [Required]
        public decimal Discount { get; set; }
        [Required]
        public string Notes { get; set; }

        public int Quantity { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        public string Key { get; set; }

        [IgnoreOnMapTo()]
        public ProductDTO Package { get; set; }

        [IgnoreOnMapTo()]
        public ProductDTO Product { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductIncludedDTO>()
                .HasOne( p => p.Product)
                .WithMany()
                .HasForeignKey(pi => pi.ProductId);

            modelBuilder.Entity<ProductIncludedDTO>()
                .HasOne(p => p.Package)
                .WithMany()
                .HasForeignKey(pi => pi.PackageId);


            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.PackageId).HasColumnOrder(2);
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.ProductId).HasColumnOrder(3);
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Discount).HasColumnOrder(4);
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Notes).HasColumnOrder(5);
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Name).HasColumnOrder(6);
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Key).HasColumnOrder(7);
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Quantity).HasColumnOrder(8);

            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Id).HasDefaultValueSql("newid()");
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Notes).HasDefaultValueSql("''");
            modelBuilder.Entity<ProductIncludedDTO>().Property(x => x.Quantity).HasDefaultValueSql("1");

            modelBuilder.Entity<ProductIncludedDTO>().HasKey(x => new { x.Id });
        }
    }
}