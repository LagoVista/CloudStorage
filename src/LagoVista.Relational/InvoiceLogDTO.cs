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

        public string EncryptedAmount { get; set; }
        public string Message { get; set; }

        [IgnoreOnMapTo()]
        public InvoiceDTO Invoice { get; set; }
        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<InvoiceLogsDTO>();

            // Relationships
            entity.HasOne(x => x.Invoice).WithMany(x => x.Logs).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.NoAction);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.InvoiceId).HasColumnOrder(2);
            entity.Property(x => x.DateStamp).HasColumnOrder(3);
            entity.Property(x => x.EventId).HasColumnOrder(4);
            entity.Property(x => x.EventData).HasColumnOrder(5);
            entity.Property(x => x.Message).HasColumnOrder(6);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.InvoiceId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.DateStamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.EventId).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.EventData).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Message).HasColumnType(StandardDBTypes.TextLong(provider));
        }
    }
}