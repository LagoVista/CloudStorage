using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    [Table("BillingEvents", Schema = "dbo")]
    public class BillingEventDTO 
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SubscriptionId { get; set; }

        public Guid ProductId { get; set; }

        /// <summary>
        /// When the billing event started
        /// </summary>
        public DateTime StartTimeStamp { get; set; }

        /// <summary>
        /// User Id of the user that initiated the billing event
        /// </summary>
        [Required]
        public String StartedByAppUserId { get; set; }

        /// <summary>
        /// When the billing event ended
        /// </summary>
        public DateTime? EndTimeStamp { get; set; }

        /// <summary>
        /// User Id of the User that Terminated the Billing Event.
        /// </summary>
        public string EndedByAppuserId { get; set; }

        /// <summary>
        /// Current Status for Billing Event, -Open, Completed, Invoiced, Error
        /// </summary>
        [Required]
        public string Status { get; set; }

        /// <summary>
        /// When the EndTimeStamp is assigned we will calculate the number of hours the resource has been
        /// used, this will be used to calculate the price/cost
        /// </summary>
        public decimal? HoursBilled { get; set; }

        /// <summary>
        /// Number of tokens consumed
        /// </summary>
        public long? Tokens { get; set; }

        /// <summary>
        /// Cost Per Unit
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Cost Per Unit
        /// </summary>
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// ShareholderType of Unit (used for calculations)
        /// </summary>
        public int UnitTypeId { get; set; }

        /// <summary>
        /// Applied Discounts
        /// </summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// Extended price for this billing period
        /// </summary>
        public decimal? Extended { get; set; }

        /// <summary>
        /// Actual resource that was used
        /// </summary>
        [Required]
        public string ResourceId { get; set; }

        /// <summary>
        /// Name of the resource that was used
        /// </summary>
        [Required]
        public string ResourceName { get; set; }

        /// <summary>
        /// Optional user entered notes
        /// </summary>
        public string Notes { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO StartedByAppUser { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO EndedByAppUser { get; set; }

        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }

        [IgnoreOnMapTo]
        public SubscriptionDTO Subscription { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<BillingEventDTO>();

            // Relationships
            entity.HasOne(x => x.Subscription).WithMany(x => x.BillingEvents).HasForeignKey(x => x.SubscriptionId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            entity.HasOne(x => x.StartedByAppUser).WithMany().HasForeignKey(x => x.StartedByAppUserId);
            entity.HasOne(x => x.EndedByAppUser).WithMany().HasForeignKey(x => x.EndedByAppuserId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Defaults
            entity.Property(x => x.ResourceName).HasDefaultValueSql(StandardDbDefaults.Text(provider, "unknown"));
            entity.Property(x => x.UnitTypeId).HasDefaultValueSql(StandardDbDefaults.One(provider));

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.ResourceId).HasColumnOrder(2);
            entity.Property(x => x.ResourceName).HasColumnOrder(3);
            entity.Property(x => x.SubscriptionId).HasColumnOrder(4);
            entity.Property(x => x.ProductId).HasColumnOrder(5);
            entity.Property(x => x.StartTimeStamp).HasColumnOrder(6);
            entity.Property(x => x.StartedByAppUserId).HasColumnOrder(7);
            entity.Property(x => x.EndTimeStamp).HasColumnOrder(8);
            entity.Property(x => x.EndedByAppuserId).HasColumnOrder(9);
            entity.Property(x => x.HoursBilled).HasColumnOrder(10);
            entity.Property(x => x.UnitCost).HasColumnOrder(11);
            entity.Property(x => x.DiscountPercent).HasColumnOrder(12);
            entity.Property(x => x.Extended).HasColumnOrder(13);
            entity.Property(x => x.UnitTypeId).HasColumnOrder(14);
            entity.Property(x => x.Notes).HasColumnOrder(15);
            entity.Property(x => x.Status).HasColumnOrder(16);
            entity.Property(x => x.UnitPrice).HasColumnOrder(17);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ResourceName).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.SubscriptionId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.StartTimeStamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.StartedByAppUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.EndTimeStamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.EndedByAppuserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.HoursBilled).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.UnitCost).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.DiscountPercent).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.Extended).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.UnitTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.UnitPrice).HasColumnType(StandardDBTypes.DecimalStorage(provider));
        }
    }
}
