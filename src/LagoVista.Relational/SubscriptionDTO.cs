using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("SUBS_KEY_{id}", IdProperty = nameof(OrganizationId), CreateIfMissing = false)]
    [Table("Subscription", Schema = "dbo")]
    public class SubscriptionDTO : DbModelBase, IEntityHeaderFactory
    {
        public const string SubscriptionKey_Trial = "trial";

        public SubscriptionDTO()
        {
        }

        public Guid? CustomerId { get; set; }

        public string PaymentTokenCustomerId { get; set; }

        public string PaymentAccountId { get; set; }

        public string PaymentAccountType { get; set; }

        public string PaymentTokenSecretId { get; set; }
        
        public bool IsActive { get; set; }

        public bool IsTrial { get; set; }

        public DateOnly? ActiveDate { get; set; }

        public DateOnly? InactiveDate { get; set; }
        public DateOnly? TrialStartDate { get; set; }

        public DateOnly? TrialExpirationDate { get; set; }

        [Required]
        public string Icon { get; set; }

        public DateOnly? PaymentTokenDate { get; set; }

        public DateOnly? PaymentTokenExpires { get; set; }

        public DateOnly Start { get; set; }

        public DateOnly? End { get; set; }


        [Required]
        public string PaymentTokenStatus { get; set; }

        [Required]
        public String Status { get; set; } = "empty";
        [Required]
        public string Key { get; set; }
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [IgnoreOnMapTo]
        public CustomerDTO Customer { get; set; }

        [IgnoreOnMapTo]
        public List<InvoiceDTO> Invoices { get; set; }

        [IgnoreOnMapTo]
        public List<BillingEventDTO> BillingEvents { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<SubscriptionDTO>();

            // Relationships
            entity.HasMany(x => x.Invoices).WithOne(x => x.Subscription).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.BillingEvents).WithOne(x => x.Subscription).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany(x => x.Subscriptions).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(3);
            entity.Property(x => x.CreationDate).HasColumnOrder(4);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(5);
            entity.Property(x => x.OrganizationId).HasColumnOrder(6);
            entity.Property(x => x.Name).HasColumnOrder(7);
            entity.Property(x => x.Key).HasColumnOrder(8);
            entity.Property(x => x.Status).HasColumnOrder(9);
            entity.Property(x => x.Description).HasColumnOrder(10);
            entity.Property(x => x.CustomerId).HasColumnOrder(11);
            entity.Property(x => x.PaymentTokenCustomerId).HasColumnOrder(12);
            entity.Property(x => x.PaymentTokenSecretId).HasColumnOrder(13);
            entity.Property(x => x.PaymentTokenDate).HasColumnOrder(14);
            entity.Property(x => x.PaymentTokenExpires).HasColumnOrder(15);
            entity.Property(x => x.PaymentTokenStatus).HasColumnOrder(16);
            entity.Property(x => x.Icon).HasColumnOrder(17);
            entity.Property(x => x.Start).HasColumnOrder(18);
            entity.Property(x => x.End).HasColumnOrder(19);
            entity.Property(x => x.PaymentAccountId).HasColumnOrder(20);
            entity.Property(x => x.PaymentAccountType).HasColumnOrder(21);
            entity.Property(x => x.IsActive).HasColumnOrder(22);
            entity.Property(x => x.ActiveDate).HasColumnOrder(23);
            entity.Property(x => x.InactiveDate).HasColumnOrder(24);
            entity.Property(x => x.TrialStartDate).HasColumnOrder(25);
            entity.Property(x => x.TrialExpirationDate).HasColumnOrder(26);
            entity.Property(x => x.IsTrial).HasColumnOrder(27);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CustomerId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PaymentTokenCustomerId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.PaymentTokenSecretId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.PaymentTokenDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.PaymentTokenExpires).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.PaymentTokenStatus).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.IconStorage(provider));
            entity.Property(x => x.Start).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.End).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.PaymentAccountId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.PaymentAccountType).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.ActiveDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.InactiveDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.TrialStartDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.TrialExpirationDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.IsTrial).HasColumnType(StandardDBTypes.FlagStorage(provider));
        }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), Name);
        }
    }
}
