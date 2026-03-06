using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    [Table("DeviceOwnerUser", Schema = "dbo")]
    public class DeviceOwnerDTO
    {
        [Key]
        public string DeviceOwnerUserId { get; set; }
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        public string FullName { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<DeviceOwnerDTO>();

            // Key / indexes / concurrency
            entity.HasKey(x => x.DeviceOwnerUserId);

            // Column order
            entity.Property(x => x.DeviceOwnerUserId).HasColumnOrder(1);
            entity.Property(x => x.Email).HasColumnOrder(2);
            entity.Property(x => x.Phone).HasColumnOrder(3);
            entity.Property(x => x.FullName).HasColumnOrder(4);
            entity.Property(x => x.CreationDate).HasColumnOrder(5);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(6);

            // Storage types
            entity.Property(x => x.DeviceOwnerUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Email).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Phone).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.FullName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}
