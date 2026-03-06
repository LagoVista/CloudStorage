using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace LagoVista.Relational
{
    [Table("Customers", Schema = "dbo")]
    public class CustomerDTO : DbModelBase, IEntityHeaderFactory
    {
        [Required]
        public string CustomerName { get; set; }
        public string BillingContactName { get; set; }
        public string BillingContactEmail { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        [Required]
        public string Notes { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        public string Zip { get; set; }

        [IgnoreOnMapTo]
        public List<InvoiceDTO> Invoices { get; set; }

        [IgnoreOnMapTo]
        public List<SubscriptionDTO> Subscriptions { get; set; }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), CustomerName);
        }


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<CustomerDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasMany(x => x.Subscriptions).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).HasPrincipalKey(x => x.AppUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).HasPrincipalKey(x => x.AppUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(x => x.Invoices).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.CustomerName).HasColumnOrder(3);
            entity.Property(x => x.BillingContactName).HasColumnOrder(4);
            entity.Property(x => x.BillingContactEmail).HasColumnOrder(5);
            entity.Property(x => x.Address1).HasColumnOrder(6);
            entity.Property(x => x.Address2).HasColumnOrder(7);
            entity.Property(x => x.City).HasColumnOrder(8);
            entity.Property(x => x.State).HasColumnOrder(9);
            entity.Property(x => x.Zip).HasColumnOrder(10);
            entity.Property(x => x.Notes).HasColumnOrder(11);
            entity.Property(x => x.CreatedById).HasColumnOrder(12);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(13);
            entity.Property(x => x.CreationDate).HasColumnOrder(14);
            entity.Property(x => x.LastUpdateDate).HasColumnOrder(15);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CustomerName).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.BillingContactName).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.BillingContactEmail).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Address1).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Address2).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.City).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.State).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Zip).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdateDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}