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
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ProductIncludedDTO>();

            // Relationships
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            entity.HasOne(x => x.Package).WithMany().HasForeignKey(x => x.PackageId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Defaults
            entity.Property(x => x.Id).HasDefaultValueSql(StandardDbDefaults.NewGuid(provider));
            entity.Property(x => x.Notes).HasDefaultValueSql(StandardDbDefaults.Text(provider, ""));
            entity.Property(x => x.Quantity).HasDefaultValueSql(StandardDbDefaults.One(provider));

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.PackageId).HasColumnOrder(2);
            entity.Property(x => x.ProductId).HasColumnOrder(3);
            entity.Property(x => x.Discount).HasColumnOrder(4);
            entity.Property(x => x.Notes).HasColumnOrder(5);
            entity.Property(x => x.Name).HasColumnOrder(6);
            entity.Property(x => x.Key).HasColumnOrder(7);
            entity.Property(x => x.Quantity).HasColumnOrder(8);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PackageId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Discount).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Quantity).HasColumnType(StandardDBTypes.IntStorage(provider));
        }
    }
}