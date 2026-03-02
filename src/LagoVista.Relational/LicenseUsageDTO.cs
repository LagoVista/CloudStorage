using Microsoft.EntityFrameworkCore;
using System;

namespace LagoVista.Relational
{
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

        public LicenseDTO License { get; set; }

        public static void Configure(ModelBuilder m)
        {
            m.Entity<LicenseUsageDTO>().HasOne(x => x.License).WithMany().HasForeignKey(x => x.LicenseId);
            m.Entity<LicenseUsageDTO>().HasKey(x => new { x.Id });

        }
    }
}
