using LagoVista.Core.Attributes;
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
    [Table("AgreementLineItems", Schema = "dbo")]
    public class AgreementLineItemDTO
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public Guid AgreementId { get; set; }

        [Required]
        public Guid ProductId { get; set; }
        [Required]
        public string ProductName { get; set; }

        public DateOnly? Start { get; set; }

        public DateOnly? End { get; set; }

        public DateOnly? NextBillingDate { get; set; }
        public DateOnly? LastBilledDate { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Extended { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Shipping { get; set; }

        public decimal Quantity { get; set; }

        public bool Taxable { get; set; }

        public int UnitTypeId { get; set; }

        public bool IsRecurring { get; set; }

        public int? RecurringCycleTypeId { get; set; }

        [IgnoreOnMapTo]
        public LicenseDTO License { get; set; }

        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }

        [IgnoreOnMapTo]
        public AgreementDTO Agreement { get; set; }

        [IgnoreOnMapTo]
        public UnitTypeDTO UnitType { get; set; }

        [IgnoreOnMapTo]
        public RecurringCycleTypeDTO RecurringCycleType { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<AgreementLineItemDTO>();

            // Relationships
            entity.HasOne(x => x.License).WithOne(x => x.AgreementLineItem).HasForeignKey<LicenseDTO>(x => x.AgreementLineItemId);
            entity.HasOne(x => x.Agreement).WithMany(x => x.LineItems).HasForeignKey(x => x.AgreementId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            entity.HasOne(x => x.UnitType).WithMany().HasForeignKey(x => x.UnitTypeId);
            entity.HasOne(x => x.RecurringCycleType).WithMany().HasForeignKey(x => x.RecurringCycleTypeId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Defaults
            entity.Property(x => x.Id).HasDefaultValueSql(StandardDbDefaults.NewGuid(provider));
            entity.Property(x => x.Shipping).HasDefaultValueSql(StandardDbDefaults.False(provider));
            entity.Property(x => x.Taxable).HasDefaultValueSql(StandardDbDefaults.False(provider));

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.AgreementId).HasColumnOrder(2);
            entity.Property(x => x.ProductId).HasColumnOrder(3);
            entity.Property(x => x.ProductName).HasColumnOrder(4);
            entity.Property(x => x.Start).HasColumnOrder(5);
            entity.Property(x => x.End).HasColumnOrder(6);
            entity.Property(x => x.UnitPrice).HasColumnOrder(7);
            entity.Property(x => x.DiscountPercent).HasColumnOrder(8);
            entity.Property(x => x.Extended).HasColumnOrder(9);
            entity.Property(x => x.SubTotal).HasColumnOrder(10);
            entity.Property(x => x.Quantity).HasColumnOrder(11);
            entity.Property(x => x.UnitTypeId).HasColumnOrder(12);
            entity.Property(x => x.IsRecurring).HasColumnOrder(13);
            entity.Property(x => x.RecurringCycleTypeId).HasColumnOrder(14);
            entity.Property(x => x.NextBillingDate).HasColumnOrder(15);
            entity.Property(x => x.LastBilledDate).HasColumnOrder(16);
            entity.Property(x => x.Taxable).HasColumnOrder(17);
            entity.Property(x => x.Shipping).HasColumnOrder(18);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.AgreementId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductName).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Start).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.End).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.UnitPrice).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.DiscountPercent).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.Extended).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.SubTotal).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.Quantity).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.UnitTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.IsRecurring).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.RecurringCycleTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.NextBillingDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.LastBilledDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.Taxable).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Shipping).HasColumnType(StandardDBTypes.DecimalStorage(provider));
        }
    }
}