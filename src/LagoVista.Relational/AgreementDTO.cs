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
    [ModernKeyId("customer-{id}", IdPath = "CustomerId")]
    [Table("Agreements",Schema ="dbo")]
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(AgreementDTO.CustomerId), CreateIfMissing = false)]
    public class AgreementDTO : DbModelBase, IEntityHeaderFactory
    {
        public Guid CustomerId { get; set; }

        [Required]
        public string CustomerContactId { get; set; }

        [Required]
        public string CustomerContactName { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Identifier { get; set; }
        public bool Locked { get; set; }
        public bool Internal { get; set; }
        public int Terms { get; set; }

        [Required]
        public string InvoicePeriod { get; set; }
        public DateOnly? Start { get; set; }
        public DateOnly? End { get; set; }
        public DateOnly? LastInvoicedDate { get; set; }
        public DateOnly? NextInvoiceDate { get; set; }

        [Required]
        public string Status { get; set; }

        public decimal? Hours { get; set; }
        public string EncryptedRate { get; set; }

        [Required]
        public string Notes { get; set; }

        public string EncryptedSubTotal { get; set; }
        public string EncryptedDiscountPercent { get; set; }
        public decimal TaxPercent { get; set; }
        public string EncryptedTax { get; set; }
        public string EncryptedShipping { get; set; }
        public string EncryptedTotal { get; set; }


        [IgnoreOnMapTo()]
        public List<TimeEntryDTO> TimeEntries { get; set; }

        [IgnoreOnMapTo()]
        public CustomerDTO Customer { get; set; }

        [IgnoreOnMapTo()]
        public List<InvoiceDTO> Invoices { get; set; }

        [IgnoreOnMapTo()]
        public List<AgreementLineItemDTO> LineItems { get; set; } = new List<AgreementLineItemDTO>();

        [IgnoreOnMapTo()]
        public List<InvoiceLineItemDTO> InvoiceLineItems { get; set; } = new List<InvoiceLineItemDTO>();

        public EntityHeader ToEntityHeader() => EntityHeader.Create(this.Id.ToString(),  this.Name);


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<AgreementDTO>();

            // Relationships
            entity.HasMany(x => x.InvoiceLineItems).WithOne().HasForeignKey(x => x.Id).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.LineItems).WithOne().HasForeignKey(x => x.AgreementId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.NoAction).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.TimeEntries).WithOne(te => te.Agreement).HasForeignKey(te => te.AgreementId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CustomerId).HasColumnOrder(2);
            entity.Property(x => x.OrganizationId).HasColumnOrder(3);
            entity.Property(x => x.Name).HasColumnOrder(4);
            entity.Property(x => x.Identifier).HasColumnOrder(5);
            entity.Property(x => x.Locked).HasColumnOrder(6);
            entity.Property(x => x.Internal).HasColumnOrder(7);
            entity.Property(x => x.InvoicePeriod).HasColumnOrder(8);
            entity.Property(x => x.Terms).HasColumnOrder(9);
            entity.Property(x => x.Start).HasColumnOrder(10);
            entity.Property(x => x.End).HasColumnOrder(11);
            entity.Property(x => x.Status).HasColumnOrder(12);
            entity.Property(x => x.Hours).HasColumnOrder(13);
            entity.Property(x => x.EncryptedRate).HasColumnOrder(14);
            entity.Property(x => x.Notes).HasColumnOrder(15);
            entity.Property(x => x.CreatedById).HasColumnOrder(16);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(17);
            entity.Property(x => x.CreationDate).HasColumnOrder(18);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(19);
            entity.Property(x => x.LastInvoicedDate).HasColumnOrder(20);
            entity.Property(x => x.NextInvoiceDate).HasColumnOrder(21);
            entity.Property(x => x.CustomerContactId).HasColumnOrder(22);
            entity.Property(x => x.CustomerContactName).HasColumnOrder(23);
            entity.Property(x => x.EncryptedSubTotal).HasColumnOrder(24);
            entity.Property(x => x.EncryptedDiscountPercent).HasColumnOrder(25);
            entity.Property(x => x.EncryptedTax).HasColumnOrder(26);
            entity.Property(x => x.EncryptedShipping).HasColumnOrder(27);
            entity.Property(x => x.EncryptedTotal).HasColumnOrder(28);
            entity.Property(x => x.TaxPercent).HasColumnOrder(29);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CustomerId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Identifier).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Locked).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Internal).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.InvoicePeriod).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.Terms).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.Start).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.End).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.Hours).HasColumnType(StandardDBTypes.DecimalStorage(provider));
            entity.Property(x => x.EncryptedRate).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastInvoicedDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.NextInvoiceDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.CustomerContactId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CustomerContactName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.EncryptedSubTotal).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedDiscountPercent).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTax).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedShipping).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotal).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.TaxPercent).HasColumnType(StandardDBTypes.DecimalSmall(provider));
        }

    }
}
