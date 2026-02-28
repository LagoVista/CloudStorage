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
    [Table("Agreements",Schema ="dbo")]
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(AgreementDTO.CustomerId), CreateIfMissing = false)]
    public class AgreementDTO : DbModelBase
    {
        public Guid CustomerId { get; set; }

        [Required]
        [IgnoreOnMapTo()]
        public string CustomerContactId { get; set; }

        [Required]
        [IgnoreOnMapTo()]
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

        [Required]
        public decimal Hours { get; set; }
        public string EncryptedRate { get; set; }

        [Required]
        public string Notes { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal Tax { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }


        [IgnoreOnMapTo()]
        public CustomerDTO Customer { get; set; }

        [IgnoreOnMapTo()]
        public List<InvoiceDTO> Invoices { get; set; }

        [IgnoreOnMapTo()]
        public List<AgreementLineItemDTO> LineItems { get; set; } = new List<AgreementLineItemDTO>();

        [IgnoreOnMapTo()]
        public List<InvoiceLineItemDTO> InvoiceLineItems { get; set; } = new List<InvoiceLineItemDTO>();

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.CustomerId).HasColumnOrder(2);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.OrganizationId).HasColumnOrder(3);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Name).HasColumnOrder(4);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Identifier).HasColumnOrder(5);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Locked).HasColumnOrder(6);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Internal).HasColumnOrder(7);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.InvoicePeriod).HasColumnOrder(8);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Terms).HasColumnOrder(9);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Start).HasColumnOrder(10);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.End).HasColumnOrder(11);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Status).HasColumnOrder(12);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Hours).HasColumnOrder(13);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.EncryptedRate).HasColumnOrder(14);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Notes).HasColumnOrder(15);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.CreatedById).HasColumnOrder(16);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(17);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.CreationDate).HasColumnOrder(18);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(19);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.LastInvoicedDate).HasColumnOrder(20);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.NextInvoiceDate).HasColumnOrder(21);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.CustomerContactId).HasColumnOrder(22);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.CustomerContactName).HasColumnOrder(23);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.SubTotal).HasColumnOrder(24);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.DiscountPercent).HasColumnOrder(25);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Tax).HasColumnOrder(26);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Shipping).HasColumnOrder(27);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Total).HasColumnOrder(28);
            modelBuilder.Entity<AgreementDTO>().Property(x => x.TaxPercent).HasColumnOrder(29);

            modelBuilder.Entity<AgreementDTO>().Property(x => x.CustomerContactId).HasDefaultValueSql("'-'");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.CustomerContactName).HasDefaultValueSql("'-'");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.DiscountPercent).HasDefaultValueSql("0");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Hours).HasDefaultValueSql("0");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.InvoicePeriod).HasDefaultValueSql("'monthly'");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Shipping).HasDefaultValueSql("0");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Status).HasDefaultValueSql("'active'");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.SubTotal).HasDefaultValueSql("0");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Tax).HasDefaultValueSql("0");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.TaxPercent).HasDefaultValueSql("0");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Terms).HasDefaultValueSql("15");
            modelBuilder.Entity<AgreementDTO>().Property(x => x.Total).HasDefaultValueSql("0");

            modelBuilder.Entity<AgreementDTO>()
            .HasMany(ps => ps.InvoiceLineItems)
            .WithOne()
            .HasForeignKey(li => li.Id);

            modelBuilder.Entity<AgreementDTO>()
            .HasMany(ps => ps.LineItems)
            .WithOne()
            .HasForeignKey(li => li.AgreementId);

            modelBuilder.Entity<AgreementDTO>()
            .HasOne(ps => ps.Customer)
            .WithMany()
            .HasForeignKey(ps => ps.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AgreementDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AgreementDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AgreementDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
