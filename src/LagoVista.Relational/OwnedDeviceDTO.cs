using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("DeviceOwnerUserDevices", Schema = "dbo")]
    public class OwnedDeviceDTO
    {
        [Key]
        public string Id { get; set; }
        [Required]
        public string DeviceUniqueId { get; set; }
        [Required]
        public string DeviceId { get; set; }
        [Required]
        public string DeviceName { get; set; }
        [Required]
        public string DeviceOwnerUserId { get; set; }
        public Guid ProductId { get; set; }
        public decimal Discount { get; set; }

        [IgnoreOnMapTo]
        public DeviceOwnerDTO Owner { get; set; }


        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<OwnedDeviceDTO>();

            // Relationships
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.DeviceOwnerUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Defaults
            entity.Property(x => x.Discount).HasDefaultValueSql(StandardDbDefaults.Zero(provider));

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.DeviceUniqueId).HasColumnOrder(2);
            entity.Property(x => x.DeviceName).HasColumnOrder(3);
            entity.Property(x => x.DeviceId).HasColumnOrder(4);
            entity.Property(x => x.DeviceOwnerUserId).HasColumnOrder(5);
            entity.Property(x => x.ProductId).HasColumnOrder(6);
            entity.Property(x => x.Discount).HasColumnOrder(7);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.DeviceUniqueId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.DeviceName).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.DeviceId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.DeviceOwnerUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Discount).HasColumnType(StandardDBTypes.DecimalSmall(provider));
        }
    }
}
