using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("user-{id}", IdPath = "UserId")]
    [Table("PaymentDeduction", Schema = "dbo")]
    public class PaymentDeductionDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PaymentId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateOnly PaymentDate { get; set; }

        [Required]
        public string TypeName { get; set; }
        [Required]
        public string TypeCode { get; set; }

        [Required]
        public string EncryptedAmount { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }

        [IgnoreOnMapTo]
        public PaymentDTO Payment { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PaymentDeductionDTO>();

            // Relationships
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Payment).WithMany(x => x.Deductions).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.PaymentId).HasColumnOrder(2);
            entity.Property(x => x.UserId).HasColumnOrder(3);
            entity.Property(x => x.PaymentDate).HasColumnOrder(4);
            entity.Property(x => x.TypeName).HasColumnOrder(5);
            entity.Property(x => x.TypeCode).HasColumnOrder(6);
            entity.Property(x => x.EncryptedAmount).HasColumnOrder(7);

            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PaymentId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.PaymentDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.TypeName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.TypeCode).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.EncryptedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
        }
    }
}
