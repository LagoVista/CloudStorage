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
    [Table("DeviceOwnerUser", Schema ="dbo")]
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
            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.DeviceOwnerUserId).HasColumnOrder(1);
            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.Email).HasColumnOrder(2);
            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.Phone).HasColumnOrder(3);
            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.FullName).HasColumnOrder(4);
            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.CreationDate).HasColumnOrder(5);
            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.LastUpdatedDate).HasColumnOrder(6);

            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.CreationDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<DeviceOwnerDTO>().Property(x => x.LastUpdatedDate).HasDefaultValueSql("getdate()");

            modelBuilder.Entity<DeviceOwnerDTO>().HasKey(x => new { x.DeviceOwnerUserId });
        }
    }
}
