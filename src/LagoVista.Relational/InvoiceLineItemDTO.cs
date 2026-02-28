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
    [Table("InvoiceLineItems" , Schema = "dbo")]
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(Invoice.CustomerId), CreateIfMissing = false)]
    public class InvoiceLineItemDTO
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InvoiceId { get; set; }
        public Guid? AgreementId { get; set; }

        public string ResourceId { get; set; }

        public string ResourceName { get; set; }

        public string ProductName { get; set; }

        public Guid? ProductId { get; set; }

        public decimal Quantity { get; set; }
        [Required]
        public string Units { get; set; }

        public bool? Taxable { get; set; }
        public string UnitPrice { get; set; }
        public string Total { get; set; }
        public string Discount { get; set; }
        public string Extended { get; set; }
        public string Shipping { get; set; }

        [IgnoreOnMapTo()]
        public InvoiceDTO Invoice { get; set; }

        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }

        [IgnoreOnMapTo]
        public AgreementDTO Agreement { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceLineItemDTO>()
            .HasOne(li => li.Invoice)
            .WithMany(inv => inv.LineItems)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLineItemDTO>()
                .HasOne(ps => ps.Agreement)
                .WithMany(a => a.InvoiceLineItems)
                .HasForeignKey(ps => ps.AgreementId);

            modelBuilder.Entity<InvoiceLineItemDTO>()
                .HasOne(ps => ps.Product)
                .WithMany()
                .HasForeignKey(ps => ps.ProductId);


            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.InvoiceId).HasColumnOrder(2);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.AgreementId).HasColumnOrder(3);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.ResourceId).HasColumnOrder(4);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.ResourceName).HasColumnOrder(5);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.ProductName).HasColumnOrder(6);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Quantity).HasColumnOrder(7);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Units).HasColumnOrder(8);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.UnitPrice).HasColumnOrder(9);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Total).HasColumnOrder(10);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Discount).HasColumnOrder(11);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Extended).HasColumnOrder(12);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Taxable).HasColumnOrder(13);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.ProductId).HasColumnOrder(14);
            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Shipping).HasColumnOrder(15);

            modelBuilder.Entity<InvoiceLineItemDTO>().Property(x => x.Taxable).HasDefaultValueSql("0");
        }
    }
}
