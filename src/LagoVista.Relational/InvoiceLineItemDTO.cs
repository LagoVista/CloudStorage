// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 036125d5de09cabcdfaecbf8fb28f0a52fb2b8f1b05c98d5d82c1947e7ac079a
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LagoVista.Relational
{

    [ModernKeyId("customer-{id}", IdPath = "CustomerId")]
    [Table("InvoiceLineItems", Schema = "dbo")]
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(Invoice.CustomerId), CreateIfMissing = false)]
    public class InvoiceLineItemDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid InvoiceId { get; set; }

        [Required] 
        public Guid CustomerId { get; set; }
        public Guid? AgreementId { get; set; }

        public string ResourceId { get; set; }

        public string ResourceName { get; set; }

        public string ProductName { get; set; }

        public Guid? ProductId { get; set; }

        public decimal Quantity { get; set; }
        [Required]
        public string Units { get; set; }

        public bool? Taxable { get; set; }
        public string EncryptedUnitPrice { get; set; }
        public string EncryptedTotal { get; set; }
        public string EncryptedDiscount { get; set; }
        public string EncryptedExtended { get; set; }
        public string EncryptedShipping { get; set; }

        [IgnoreOnMapTo()]
        public InvoiceDTO Invoice { get; set; }

        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }

        [IgnoreOnMapTo]
        public AgreementDTO Agreement { get; set; }

        [IgnoreOnMapTo]
        public CustomerDTO Customer { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<InvoiceLineItemDTO>();

            // Relationships
            entity.HasOne(x => x.Invoice).WithMany(x => x.LineItems).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Agreement).WithMany(x => x.InvoiceLineItems).HasForeignKey(x => x.AgreementId);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.InvoiceId).HasColumnOrder(2);
            entity.Property(x => x.InvoiceId).HasColumnOrder(3);
            entity.Property(x => x.AgreementId).HasColumnOrder(4);
            entity.Property(x => x.ResourceId).HasColumnOrder(5);
            entity.Property(x => x.ResourceName).HasColumnOrder(6);
            entity.Property(x => x.ProductName).HasColumnOrder(7);
            entity.Property(x => x.Quantity).HasColumnOrder(8);
            entity.Property(x => x.Units).HasColumnOrder(9);
            entity.Property(x => x.EncryptedUnitPrice).HasColumnOrder(10);
            entity.Property(x => x.EncryptedTotal).HasColumnOrder(11);
            entity.Property(x => x.EncryptedDiscount).HasColumnOrder(12);
            entity.Property(x => x.EncryptedExtended).HasColumnOrder(13);
            entity.Property(x => x.Taxable).HasColumnOrder(14);
            entity.Property(x => x.ProductId).HasColumnOrder(15);
            entity.Property(x => x.EncryptedShipping).HasColumnOrder(16);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.InvoiceId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.AgreementId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CustomerId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ResourceId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ResourceName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.ProductName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Quantity).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.Units).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.EncryptedUnitPrice).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotal).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedDiscount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedExtended).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Taxable).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.ProductId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.EncryptedShipping).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
        }
    }
}
