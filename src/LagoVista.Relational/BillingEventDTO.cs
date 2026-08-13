using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LagoVista.Relational
{
    /// <summary>
    /// Note this class is also in the BillingEvent class in the billing project, but we don't want to have a dependency on that project here, so we duplicate it.
    /// </summary>

    public static class BillingEventRollupTypes
    {
        public const string Detail = "Detail";
        public const string Hourly = "Hourly";
        public const string Daily = "Daily";
        public const string Monthly = "Monthly";
    }

    [Table("BillingEvents", Schema = "dbo")]
    public class BillingEventDTO 
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SubscriptionId { get; set; }

        public Guid ProductId { get; set; }

        public Guid? ModelUsageRateId { get; set; }


        /// <summary>
        /// When the billing event started
        /// </summary>
        public DateTime StartTimestamp { get; set; }

        /// <summary>
        /// User Id of the user that initiated the billing event
        /// </summary>
        [Required]
        public String StartedByAppUserId { get; set; }

        /// <summary>
        /// When the billing event ended
        /// </summary>
        public DateTime? EndTimestamp { get; set; }
        
        /// <summary>
        /// User Id of the User that Terminated the Billing Event.
        /// </summary>
        public string EndedByAppUserId { get; set; }

        /// <summary>
        /// That the record should be considered from a billing perspective. 
        /// </summary>
        public DateOnly? BillingDate { get; set; }

        /// <summary>
        /// UTC instant when this open slice must roll into the next billing day.
        /// Null only for event types that never roll.
        /// </summary>
        public DateTime? RolloverAt { get; set; }

        /// <summary>
        /// Stable business key used to safely de-duplicate usage events submitted through the billing event pipeline.
        /// This is not the RabbitMQ message id; retries for the same usage slice should reuse this value.
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// Captured billing timezone for this slice.
        /// This must be a stable id into our supported timezone catalog.
        /// </summary>
        public int BillingTimeZoneId { get; set; }

        /// <summary>
        /// Current Status for Billing Event, -Open, Completed, Invoiced, Error
        /// </summary>
        [Required]
        public string Status { get; set; }

        /// <summary>
        /// When the EndTimestamp is assigned we will calculate the number of hours the resource has been
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
        /// Total Consumed Custom calculated cost for this billing period, this is the actual cost of the resource used, not the price charged to the customer
        /// </summary>
        public decimal? ActualCost { get; set; }

        /// <summary>
        /// ShareholderType of Unit (used for calculations)
        /// </summary>
        public int UnitTypeId { get; set; }

        /// <summary>
        /// Applied Discounts
        /// </summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// EncryptedExtended price for this billing period
        /// </summary>
        public decimal? Extended { get; set; }

        /// <summary>
        /// Usage key to identify costs on usage events.
        /// </summary>
        public string VendorUsageKey { get; set; }

        /// <summary>
        /// Quantity of the resource that was used
        /// </summary>
        public decimal? Quantity { get; set; }

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

        [IgnoreOnMapTo]
        public ModelUsageRateDTO ModelUsageRate { get; set; }

        /// <summary>
        /// Defines the storage grain represented by this billing event.
        /// Valid values are Detail, Hourly, Daily, and Monthly.
        /// </summary>
        [Required]
        public string RollupType { get; set; } = BillingEventRollupTypes.Detail;

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<BillingEventDTO>();

            // Relationships
            entity.HasOne(x => x.Subscription).WithMany(x => x.BillingEvents).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StartedByAppUser).WithMany().HasForeignKey(x => x.StartedByAppUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EndedByAppUser).WithMany().HasForeignKey(x => x.EndedByAppUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ModelUsageRate).WithMany().HasForeignKey(x => x.ModelUsageRateId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.ResourceId).HasColumnOrder(2);
            entity.Property(x => x.ResourceName).HasColumnOrder(3);
            entity.Property(x => x.SubscriptionId).HasColumnOrder(4);
            entity.Property(x => x.ProductId).HasColumnOrder(5);
            entity.Property(x => x.StartTimestamp).HasColumnOrder(6);
            entity.Property(x => x.StartedByAppUserId).HasColumnOrder(7);
            entity.Property(x => x.EndTimestamp).HasColumnOrder(8);
            entity.Property(x => x.RolloverAt).HasColumnOrder(9);
            entity.Property(x => x.BillingTimeZoneId).HasColumnOrder(10);
            entity.Property(x => x.BillingDate).HasColumnOrder(11);
            entity.Property(x => x.EndedByAppUserId).HasColumnOrder(12);
            entity.Property(x => x.HoursBilled).HasColumnOrder(13);
            entity.Property(x => x.UnitCost).HasColumnOrder(14);
            entity.Property(x => x.DiscountPercent).HasColumnOrder(15);
            entity.Property(x => x.Extended).HasColumnOrder(16);
            entity.Property(x => x.UnitTypeId).HasColumnOrder(17);
            entity.Property(x => x.Notes).HasColumnOrder(18);
            entity.Property(x => x.Status).HasColumnOrder(19);
            entity.Property(x => x.UnitPrice).HasColumnOrder(20);
            entity.Property(x => x.Tokens).HasColumnOrder(21);
            entity.Property(x => x.IdempotencyKey).HasColumnOrder(22);
            entity.Property(x => x.RollupType).HasColumnOrder(23);
            entity.Property(x => x.Quantity).HasColumnOrder(24);
            entity.Property(x => x.VendorUsageKey).HasColumnOrder(25);
            entity.Property(x => x.ActualCost).HasColumnOrder(26);
            entity.Property(x => x.ModelUsageRateId).HasColumnOrder(27);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ResourceName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.SubscriptionId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.StartTimestamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.StartedByAppUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.EndTimestamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.RolloverAt).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.BillingTimeZoneId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.BillingDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.EndedByAppUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.HoursBilled).HasColumnType(StandardDBTypes.DecimalMedium(provider));
            entity.Property(x => x.UnitCost).HasColumnType(StandardDBTypes.MoneyStoragePrecise(provider));
            entity.Property(x => x.DiscountPercent).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.Extended).HasColumnType(StandardDBTypes.MoneyStoragePrecise(provider));
            entity.Property(x => x.UnitTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.UnitPrice).HasColumnType(StandardDBTypes.MoneyStoragePrecise(provider));
            entity.Property(x => x.Tokens).HasColumnType(StandardDBTypes.LongStorage(provider));
            entity.Property(x => x.IdempotencyKey).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.RollupType).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.Quantity).HasColumnType(StandardDBTypes.DecimalMedium(provider));
            entity.Property(x => x.VendorUsageKey).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.ActualCost).HasColumnType(StandardDBTypes.MoneyStoragePrecise(provider));
            entity.Property(x => x.ModelUsageRateId).HasColumnType(StandardDBTypes.UuidStorage(provider));

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_BillingEvents_RollupType",
                    $"{nameof(BillingEventDTO.RollupType)} IN ('Detail', 'Hourly', 'Daily', 'Monthly')");
            });
        }
    }
}
