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
    [ModernKeyId("customer-{id}", IdPath = "CustomerId")]
    [EncryptionKey("AgreementLineItem-{id}", IdProperty = nameof(AgreementId), CreateIfMissing = false)]
    [Table("AgreementLineItems", Schema = "dbo")]
    public class AgreementLineItemDTO
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public Guid AgreementId { get; set; }
        [Required]
        public Guid CustomerId { get; set; }
        [Required]
        public Guid ProductId { get; set; }
        [Required]
        public string ProductName { get; set; }

        public DateOnly? Start { get; set; }

        public DateOnly? End { get; set; }

        public DateOnly? NextBillingDate { get; set; }
        public DateOnly? LastBilledDate { get; set; }

        public string EncryptedUnitPrice { get; set; }
        public string EncryptedDiscountPercent { get; set; }
        public string EncryptedExtended { get; set; }

        public string EncryptedSubTotal { get; set; }

        public string EncryptedShipping { get; set; }

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
        [IgnoreOnMapTo]
        public CustomerDTO Customer { get; set; }

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
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.AgreementId).HasColumnOrder(2);
            entity.Property(x => x.ProductId).HasColumnOrder(3);
            entity.Property(x => x.CustomerId).HasColumnOrder(4);
            entity.Property(x => x.ProductName).HasColumnOrder(5);
            entity.Property(x => x.Start).HasColumnOrder(6);
            entity.Property(x => x.End).HasColumnOrder(7);
            entity.Property(x => x.EncryptedUnitPrice).HasColumnOrder(8);
            entity.Property(x => x.EncryptedDiscountPercent).HasColumnOrder(9);
            entity.Property(x => x.EncryptedExtended).HasColumnOrder(10);
            entity.Property(x => x.EncryptedSubTotal).HasColumnOrder(11);
            entity.Property(x => x.Quantity).HasColumnOrder(12);
            entity.Property(x => x.UnitTypeId).HasColumnOrder(13);
            entity.Property(x => x.IsRecurring).HasColumnOrder(14);
            entity.Property(x => x.RecurringCycleTypeId).HasColumnOrder(15);
            entity.Property(x => x.NextBillingDate).HasColumnOrder(16);
            entity.Property(x => x.LastBilledDate).HasColumnOrder(17);
            entity.Property(x => x.Taxable).HasColumnOrder(18);
            entity.Property(x => x.EncryptedShipping).HasColumnOrder(19);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.AgreementId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CustomerId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ProductName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Start).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.End).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.EncryptedUnitPrice).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedDiscountPercent).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedExtended).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedSubTotal).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Quantity).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.UnitTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.IsRecurring).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.RecurringCycleTypeId).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.NextBillingDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.LastBilledDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.Taxable).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.EncryptedShipping).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
        }
    }
}