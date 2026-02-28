using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Invoice", Schema = "dbo")]
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(CustomerId), CreateIfMissing = false)]
    public class InvoiceDTO
    {
        [Key]
        public Guid Id { get; set; }


        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int InvoiceNumber { get; set; }

        public bool IsMaster { get; set; }
        public bool HasChildren { get; set; }

        public Guid? SubscriptionId { get; set; }
        public Guid? AgreementId { get; set; }
        public Guid? CustomerId { get; set; }

        public Guid? MasterInvoiceId { get; set; }

        public string ContactId { get; set; }

        public String OrgId { get; set; }
        public DateOnly BillingStart { get; set; }
        public DateOnly BillingEnd { get; set; }

        public DateTime CreationTimeStamp { get; set; }
        public DateOnly DueDate { get; set; }
        public String Total { get; set; }
        public String Discount { get; set; }
        public String Extended { get; set; }
        public String TotalPaid { get; set; }

        [Required]
        public string Tax { get; set; }
        
        [Required]
        public decimal TaxPercent { get; set; }
        [Required]
        public string Shipping { get; set; }
        [Required]
        public string Subtotal { get; set; }


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

        public DateTime StatusDate { get; set; }

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


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceDTO>()
            .HasOne(p => p.Subscription)
            .WithMany(s => s.Invoices)
            .HasForeignKey(p => p.SubscriptionId);

            modelBuilder.Entity<InvoiceDTO>()
            .HasOne(inv => inv.Agreement)
            .WithMany(agr => agr.Invoices)
            .HasForeignKey(inv => inv.AgreementId);

            modelBuilder.Entity<InvoiceDTO>()
            .HasOne(inv => inv.Customer)
            .WithMany(cst => cst.Invoices)
            .HasForeignKey(li => li.CustomerId);

            modelBuilder.Entity<InvoiceDTO>()
            .HasOne(inv => inv.Organization)
            .WithMany()
            .HasForeignKey(li => li.OrgId);

            modelBuilder.Entity<InvoiceDTO>()
            .HasMany(inv => inv.Logs)
            .WithOne(i => i.Invoice)
            .HasForeignKey(i => i.InvoiceId);

            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.IsMaster).HasColumnOrder(2);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.MasterInvoiceId).HasColumnOrder(3);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.HasChildren).HasColumnOrder(4);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.InvoiceNumber).HasColumnOrder(5);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.SubscriptionId).HasColumnOrder(6);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.OrgId).HasColumnOrder(7);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.CustomerId).HasColumnOrder(8);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Notes).HasColumnOrder(9);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.BillingStart).HasColumnOrder(10);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.BillingEnd).HasColumnOrder(11);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.CreationTimeStamp).HasColumnOrder(12);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.DueDate).HasColumnOrder(13);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Total).HasColumnOrder(14);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Discount).HasColumnOrder(15);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Extended).HasColumnOrder(16);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.TotalPaid).HasColumnOrder(17);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.PaidDate).HasColumnOrder(18);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.ClosedTransactionId).HasColumnOrder(19);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Status).HasColumnOrder(20);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.StatusDate).HasColumnOrder(21);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.FailedAttemptCount).HasColumnOrder(22);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.AgreementId).HasColumnOrder(23);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Shipping).HasColumnOrder(24);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Tax).HasColumnOrder(25);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Subtotal).HasColumnOrder(26);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.TaxPercent).HasColumnOrder(27);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.ContactId).HasColumnOrder(28);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.AdditionalNotes).HasColumnOrder(29);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.IsLocked).HasColumnOrder(30);
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.InvoiceDate).HasColumnOrder(31);

            modelBuilder.Entity<InvoiceDTO>().Property(x => x.AdditionalNotes).HasDefaultValueSql("''");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.CreationTimeStamp).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.FailedAttemptCount).HasDefaultValueSql("0");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.HasChildren).HasDefaultValueSql("0");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.IsLocked).HasDefaultValueSql("0");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.IsMaster).HasDefaultValueSql("1");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Shipping).HasDefaultValueSql("''");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Status).HasDefaultValueSql("'New'");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.StatusDate).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Subtotal).HasDefaultValueSql("''");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Tax).HasDefaultValueSql("''");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.TaxPercent).HasDefaultValueSql("0");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Total).HasDefaultValueSql("0");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.TotalPaid).HasDefaultValueSql("0");

            modelBuilder.Entity<InvoiceDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<InvoiceDTO>().Property(x => x.AdditionalNotes).HasColumnType("varchar(max)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.AgreementId).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.BillingEnd).HasColumnType("date");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.BillingStart).HasColumnType("date");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.ClosedTransactionId).HasColumnType("varchar(255)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.ContactId).HasColumnType("varchar(32)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.CreationTimeStamp).HasColumnType("datetime2(7)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.CustomerId).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Discount).HasColumnType("varchar(1024)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.DueDate).HasColumnType("date");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Extended).HasColumnType("varchar(1024)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.FailedAttemptCount).HasColumnType("int");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.HasChildren).HasColumnType("bit");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Id).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.InvoiceDate).HasColumnType("date");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.InvoiceNumber).HasColumnType("int");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.IsLocked).HasColumnType("bit");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.IsMaster).HasColumnType("bit");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.MasterInvoiceId).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Notes).HasColumnType("varchar(max)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.OrgId).HasColumnType("varchar(32)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.PaidDate).HasColumnType("date");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Shipping).HasColumnType("varchar(1024)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Status).HasColumnType("varchar(50)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.StatusDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.SubscriptionId).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Subtotal).HasColumnType("varchar(1024)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Tax).HasColumnType("varchar(1024)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.TaxPercent).HasColumnType("decimal(6,2)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.Total).HasColumnType("varchar(1024)");
            modelBuilder.Entity<InvoiceDTO>().Property(x => x.TotalPaid).HasColumnType("varchar(1024)");
        }
    }
}
