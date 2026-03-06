using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Emit;

namespace LagoVista.Relational
{
    [Table("Licenses", Schema = "dbo")]
    public class LicenseDTO : DbModelBase, IEntityHeaderFactory
    {
        [Required]
        public string Name { get; set; }

        public Guid CustomerId { get; set; }

        public Guid AgreementLineItemId { get; set; }
        public bool IsActive { get; set; }
 
        public DateOnly ActiveDate { get; set; }
        public DateOnly RenewalDate { get; set; }
        public decimal QuantityUsed { get; set; }
        public decimal QuantityAllocated { get; set; }

        [IgnoreOnMapTo]
        public List<LicenseUsageDTO> Usage { get; set; }

        [IgnoreOnMapTo]
        public CustomerDTO Customer { get; set; }

        [IgnoreOnMapTo]
        public AgreementLineItemDTO AgreementLineItem { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<LicenseDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(x => x.Usage).WithOne(x => x.License).HasForeignKey(x => x.LicenseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AgreementLineItem).WithOne(x => x.License).HasForeignKey<LicenseDTO>(x => x.AgreementLineItemId).OnDelete(DeleteBehavior.Cascade);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(3);
            entity.Property(x => x.OrganizationId).HasColumnOrder(4);
            entity.Property(x => x.CreationDate).HasColumnOrder(5);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(6);
            entity.Property(x => x.CustomerId).HasColumnOrder(7);
            entity.Property(x => x.AgreementLineItemId).HasColumnOrder(8);
            entity.Property(x => x.Name).HasColumnOrder(9);
            entity.Property(x => x.IsActive).HasColumnOrder(10);
            entity.Property(x => x.ActiveDate).HasColumnOrder(11);
            entity.Property(x => x.RenewalDate).HasColumnOrder(12);
            entity.Property(x => x.QuantityUsed).HasColumnOrder(13);
            entity.Property(x => x.QuantityAllocated).HasColumnOrder(14);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.CustomerId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.AgreementLineItemId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.ActiveDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.RenewalDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.QuantityUsed).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.QuantityAllocated).HasColumnType(StandardDBTypes.DecimalStorage(provider));
        }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(),Name);
        }
    }
}
