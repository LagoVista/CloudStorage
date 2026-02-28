using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("DeviceOwnerUserDevices", Schema ="dbo")]
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
            modelBuilder.Entity<OwnedDeviceDTO>()
            .HasOne(od => od.Owner)
            .WithMany()
            .HasForeignKey(od => od.DeviceOwnerUserId);

            modelBuilder.Entity<OwnedDeviceDTO>()
            .HasOne(od => od.Product)
            .WithMany()
            .HasForeignKey(od => od.ProductId);


            modelBuilder.Entity<OwnedDeviceDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<OwnedDeviceDTO>().Property(x => x.DeviceUniqueId).HasColumnOrder(2);
            modelBuilder.Entity<OwnedDeviceDTO>().Property(x => x.DeviceName).HasColumnOrder(3);
            modelBuilder.Entity<OwnedDeviceDTO>().Property(x => x.DeviceId).HasColumnOrder(4);
            modelBuilder.Entity<OwnedDeviceDTO>().Property(x => x.DeviceOwnerUserId).HasColumnOrder(5);
            modelBuilder.Entity<OwnedDeviceDTO>().Property(x => x.ProductId).HasColumnOrder(6);
            modelBuilder.Entity<OwnedDeviceDTO>().Property(x => x.Discount).HasColumnOrder(7);
        }
    }
}
