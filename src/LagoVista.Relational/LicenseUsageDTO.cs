using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("LicenseUsage", Schema = "dbo")] 
    public class LicenseUsageDTO
    {
        public Guid Id { get; set; }
        public Guid LicenseId { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal QtyAdded { get; set; }
        public decimal QtyRemoved { get; set; }
        public string DeviceUniqueId { get; set; }
        public string DeviceId { get; set; }
        public string Notes { get; set; }

        [IgnoreOnMapTo]
        public LicenseDTO License { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<LicenseUsageDTO>();

            // Relationships
            entity.HasOne(x => x.License).WithMany(x => x.Usage).HasForeignKey(x => x.LicenseId).OnDelete(DeleteBehavior.NoAction);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.LicenseId).HasColumnOrder(2);
            entity.Property(x => x.TimeStamp).HasColumnOrder(3);
            entity.Property(x => x.QtyAdded).HasColumnOrder(4);
            entity.Property(x => x.QtyRemoved).HasColumnOrder(5);
            entity.Property(x => x.DeviceUniqueId).HasColumnOrder(6);
            entity.Property(x => x.DeviceId).HasColumnOrder(7);
            entity.Property(x => x.Notes).HasColumnOrder(8);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.LicenseId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.TimeStamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.QtyAdded).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.QtyRemoved).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.DeviceUniqueId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.DeviceId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
        }
    }
}
