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
    [ModernKeyId("customer-{id}", IdPath = "CustomerId")]
    [Table("Invoice", Schema = "dbo")]
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(CustomerId), CreateIfMissing = false)]
    public class InvoiceDTO : IEntityHeaderFactory
    {
        [Key]
        public Guid Id { get; set; }

        public int InvoiceNumber { get; set; }

        public bool IsMaster { get; set; }
        public bool HasChildren { get; set; }

        public Guid? SubscriptionId { get; set; }
        public Guid? AgreementId { get; set; }
        public Guid CustomerId { get; set; }

        public Guid? MasterInvoiceId { get; set; }

        public string ContactId { get; set; }

        public string ContactName { get; set; } 

        [Required]
        public String OrganizationId { get; set; }
        public DateOnly BillingStart { get; set; }
        public DateOnly BillingEnd { get; set; }
        public DateOnly ServicesStart { get; set; }
        public DateOnly ServicesEnd { get; set; }

        public DateTime CreationTimestamp { get; set; }
        public DateOnly DueDate { get; set; }
        public String EncryptedTotal { get; set; }
        public String EncryptedDiscount { get; set; }
        public String EncryptedExtended { get; set; }
        public String EncryptedTotalPaid { get; set; }

        [Required]
        public string EncryptedTax { get; set; }

        [Required]
        public decimal TaxPercent { get; set; }
        [Required]
        public string EncryptedShipping { get; set; }
        [Required]
        public string EncryptedSubtotal { get; set; }


        public bool IsLocked { get; set; }

        public DateOnly? PaidDate { get; set; }

        public DateOnly InvoiceDate { get; set; }

        public int FailedAttemptCount { get; set; }

        public string ClosedTransactionId { get; set; }

        public string Notes { get; set; }

        [Required]
        public string AdditionalNotes { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime StatusTimestamp { get; set; }

        [IgnoreOnMapTo]
        public CustomerDTO Customer { get; set; }

        [IgnoreOnMapTo]
        public AgreementDTO Agreement { get; set; }

        [MapTo("OwnerOrganization")]
        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }

        [IgnoreOnMapTo]
        public SubscriptionDTO Subscription { get; set; }

        [IgnoreOnMapTo]
        public List<InvoiceLineItemDTO> LineItems { get; set; }

        [IgnoreOnMapTo]
        public List<InvoiceLogsDTO> Logs { get; set; }

        [IgnoreOnMapTo]
        [NotMapped]
        public string CustomerName => Customer?.CustomerName;

        [IgnoreOnMapTo]
        [NotMapped]
        public string OrgName => Organization?.OrgName;

        [IgnoreOnMapTo]
        [NotMapped]
        public string AgreementName => Agreement?.Name;

 

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<InvoiceDTO>();

            // Relationships
            entity.HasOne(x => x.Subscription).WithMany(x => x.Invoices).HasForeignKey(x => x.SubscriptionId);
            entity.HasOne(x => x.Agreement).WithMany(x => x.Invoices).HasForeignKey(x => x.AgreementId);
            entity.HasOne(x => x.Customer).WithMany(x => x.Invoices).HasForeignKey(x => x.CustomerId);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasMany(x => x.Logs).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.IsMaster).HasColumnOrder(2);
            entity.Property(x => x.MasterInvoiceId).HasColumnOrder(3);
            entity.Property(x => x.HasChildren).HasColumnOrder(4);
            entity.Property(x => x.InvoiceNumber).HasColumnOrder(5);
            entity.Property(x => x.SubscriptionId).HasColumnOrder(6);
            entity.Property(x => x.OrganizationId).HasColumnOrder(7);
            entity.Property(x => x.CustomerId).HasColumnOrder(8);
            entity.Property(x => x.Notes).HasColumnOrder(9);
            entity.Property(x => x.BillingStart).HasColumnOrder(10);
            entity.Property(x => x.BillingEnd).HasColumnOrder(11);
            entity.Property(x => x.ServicesStart).HasColumnOrder(12);
            entity.Property(x => x.ServicesEnd).HasColumnOrder(13);
            entity.Property(x => x.CreationTimestamp).HasColumnOrder(14);
            entity.Property(x => x.DueDate).HasColumnOrder(15);
            entity.Property(x => x.EncryptedTotal).HasColumnOrder(16);
            entity.Property(x => x.EncryptedDiscount).HasColumnOrder(17);
            entity.Property(x => x.EncryptedExtended).HasColumnOrder(18);
            entity.Property(x => x.EncryptedTotalPaid).HasColumnOrder(19);
            entity.Property(x => x.PaidDate).HasColumnOrder(20);
            entity.Property(x => x.ClosedTransactionId).HasColumnOrder(21);
            entity.Property(x => x.Status).HasColumnOrder(22);
            entity.Property(x => x.StatusTimestamp).HasColumnOrder(23);
            entity.Property(x => x.FailedAttemptCount).HasColumnOrder(24);
            entity.Property(x => x.AgreementId).HasColumnOrder(25);
            entity.Property(x => x.EncryptedShipping).HasColumnOrder(26);
            entity.Property(x => x.EncryptedTax).HasColumnOrder(27);
            entity.Property(x => x.EncryptedSubtotal).HasColumnOrder(28);
            entity.Property(x => x.TaxPercent).HasColumnOrder(29);
            entity.Property(x => x.ContactId).HasColumnOrder(30);
            entity.Property(x => x.ContactName).HasColumnOrder(31);
            entity.Property(x => x.AdditionalNotes).HasColumnOrder(32);
            entity.Property(x => x.IsLocked).HasColumnOrder(33);
            entity.Property(x => x.InvoiceDate).HasColumnOrder(34);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.IsMaster).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.MasterInvoiceId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.HasChildren).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.InvoiceNumber).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.SubscriptionId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CustomerId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.BillingStart).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.BillingEnd).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.ServicesStart).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.ServicesEnd).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.CreationTimestamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.DueDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.EncryptedTotal).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedDiscount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedExtended).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalPaid).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.PaidDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.ClosedTransactionId).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.StatusTimestamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.FailedAttemptCount).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.AgreementId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.EncryptedShipping).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTax).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedSubtotal).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.TaxPercent).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.ContactId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ContactName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.AdditionalNotes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.IsLocked).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.InvoiceDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
        }
        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), InvoiceNumber.ToString());
        }
    }
}