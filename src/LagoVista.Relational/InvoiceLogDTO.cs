// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: d3714c8aba32044de83feac0abf2e177ef3b7e5554b20667f9d117e422826b80
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("InvoiceLogs", Schema = "dbo")]
    public class InvoiceLogsDTO
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InvoiceId { get; set; }

        public DateTime DateStamp { get; set; }

        [Required]
        public string EventId { get; set; }
        public string EventData { get; set; }
        public string Message { get; set; }

        [IgnoreOnMapTo()]
        public InvoiceDTO Invoice { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceLogsDTO>()
                .HasOne(l => l.Invoice)
                .WithMany(inv => inv.Logs)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLogsDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<InvoiceLogsDTO>().Property(x => x.InvoiceId).HasColumnOrder(2);
            modelBuilder.Entity<InvoiceLogsDTO>().Property(x => x.DateStamp).HasColumnOrder(3);
            modelBuilder.Entity<InvoiceLogsDTO>().Property(x => x.EventId).HasColumnOrder(4);
            modelBuilder.Entity<InvoiceLogsDTO>().Property(x => x.EventData).HasColumnOrder(5);
            modelBuilder.Entity<InvoiceLogsDTO>().Property(x => x.Message).HasColumnOrder(6);

            modelBuilder.Entity<InvoiceLogsDTO>().Property(x => x.DateStamp).HasDefaultValueSql("getdate()");
        }
    }
}