using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("ModelUsageRates", Schema = "dbo")]
    public class ModelUsageRateDTO
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Stable key identifying the vendor, model, and billable usage dimension.
        /// Examples:
        /// openai:gpt-5.6-luna:input
        /// openai:gpt-5.6-luna:cached-input
        /// openai:gpt-5.6-luna:output
        /// xai:grok-4:code-execution
        /// </summary>
        [Required]
        public string VendorUsageKey { get; set; }

        /// <summary>
        /// Friendly name used when maintaining and reviewing the usage rate.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Additional information about the rate or vendor billing dimension.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type of unit used to normalize the usage quantity.
        /// Examples include per 1,000 tokens, per 100,000 tokens,
        /// per transaction, per second, or per hour.
        /// </summary>
        public int UnitTypeId { get; set; }

        /// <summary>
        /// Cost charged by the vendor per normalized unit.
        /// </summary>
        public decimal UnitCost { get; set; }

        /// <summary>
        /// UTC instant at which this rate becomes effective.
        /// </summary>
        public DateTime EffectiveFromUtc { get; set; }

        /// <summary>
        /// UTC instant at which this rate is no longer effective.
        /// Null indicates that the rate remains effective indefinitely.
        /// </summary>
        public DateTime? EffectiveToUtc { get; set; }

        /// <summary>
        /// Indicates whether this rate can currently be used when resolving costs.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional notes about the source or maintenance of this rate.
        /// </summary>
        public string Notes { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ModelUsageRateDTO>();

            // Key / indexes
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.VendorUsageKey,
                x.EffectiveFromUtc
            })
            .IsUnique()
            .HasDatabaseName("UX_ModelUsageRates_VendorUsageKey_EffectiveFromUtc");

            entity.HasIndex(x => new
            {
                x.VendorUsageKey,
                x.IsActive
            })
            .HasDatabaseName("IX_ModelUsageRates_VendorUsageKey_IsActive");

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.VendorUsageKey).HasColumnOrder(2);
            entity.Property(x => x.Name).HasColumnOrder(3);
            entity.Property(x => x.Description).HasColumnOrder(4);
            entity.Property(x => x.UnitTypeId).HasColumnOrder(5);
            entity.Property(x => x.UnitCost).HasColumnOrder(6);
            entity.Property(x => x.EffectiveFromUtc).HasColumnOrder(7);
            entity.Property(x => x.EffectiveToUtc).HasColumnOrder(8);
            entity.Property(x => x.IsActive).HasColumnOrder(9);
            entity.Property(x => x.Notes).HasColumnOrder(10);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.VendorUsageKey).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.UnitTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.UnitCost).HasColumnType(StandardDBTypes.MoneyStoragePrecise(provider));
            entity.Property(x => x.EffectiveFromUtc).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.EffectiveToUtc).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_ModelUsageRates_EffectiveDates",
                    $"{nameof(ModelUsageRateDTO.EffectiveToUtc)} IS NULL OR " +
                    $"{nameof(ModelUsageRateDTO.EffectiveToUtc)} > {nameof(ModelUsageRateDTO.EffectiveFromUtc)}");

                table.HasCheckConstraint(
                    "CK_ModelUsageRates_UnitCost",
                    $"{nameof(ModelUsageRateDTO.UnitCost)} >= 0");
            });
        }
    }
}