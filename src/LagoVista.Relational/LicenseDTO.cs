using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace LagoVista.Relational
{
    public class LicenseDTO : DbModelBase, IEntityHeaderFactory
    {
        public string Name { get; set; }

        public EntityHeader Customer { get; set; }

        public Guid AgreementLineItemId { get; set; }
        public bool IsActive { get; set; }
 
        public DateOnly ActiveDate { get; set; }
        public DateOnly RenewalDate { get; set; }
        public decimal QuantityUsed { get; set; }
        public decimal QuantityAllocated { get; set; }
        public List<LicenseUsageDTO> Usage { get; set; }


        public AgreementLineItemDTO AgreementLineItem { get; set; }

        public static void Configure(ModelBuilder m)
        {
           m.Entity<LicenseDTO>()
                .HasMany(l => l.Usage)
                .WithOne(a => a.License)
                .HasForeignKey(l => l.LicenseId)
                .OnDelete(DeleteBehavior.Cascade);  

            m.Entity<LicenseDTO>()
                .HasOne(l => l.AgreementLineItem)
                .WithOne(a => a.License)
                .HasForeignKey<LicenseDTO>(li => li.AgreementLineItemId)
                .OnDelete(DeleteBehavior.Cascade);

            m.Entity<LicenseDTO>().HasKey(x => new { x.Id });
        }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(),Name);
        }
    }
}
