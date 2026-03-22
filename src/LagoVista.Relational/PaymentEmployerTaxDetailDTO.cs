using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("org-{id}", IdPath = "OrganizationId")]
    [Table("PaymentEmployerTaxDetail", Schema = "dbo")]
    [EncryptionKey("PAYROLL_KEY")]
    public class PaymentEmployerTaxDetailDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PaymentId { get; set; }

        [Required]
        public Guid PayrollRunId { get; set; }

        [Required]
        public DateOnly EffectiveDate { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string OrganizationId { get; set; }

        [Required]
        public string TypeName { get; set; }

        [Required]
        public string TypeCode { get; set; }

        [Required]
        public string JurisdictionCode { get; set; }

        [Required]
        public string EncryptedTaxableWages { get; set; }

        [Required]
        public string EncryptedAmount { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }

        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }

        [IgnoreOnMapTo]
        public PaymentDTO Payment { get; set; }

        [IgnoreOnMapTo]
        public PayrollRunDTO PayrollRun { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PaymentEmployerTaxDetailDTO>();

            // Relationships
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Payment).WithMany(x => x.PayrollTaxDetails).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PayrollRun).WithMany(x => x.PayrollTaxDetails).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade).OnDelete(DeleteBehavior.Cascade);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.PaymentId).HasColumnOrder(2);
            entity.Property(x => x.PayrollRunId).HasColumnOrder(3);
            entity.Property(x => x.OrganizationId).HasColumnOrder(4);
            entity.Property(x => x.UserId).HasColumnOrder(5);
            entity.Property(x => x.EffectiveDate).HasColumnOrder(6);
            entity.Property(x => x.TypeName).HasColumnOrder(7);
            entity.Property(x => x.TypeCode).HasColumnOrder(8);
            entity.Property(x => x.JurisdictionCode).HasColumnOrder(9);
            entity.Property(x => x.EncryptedTaxableWages).HasColumnOrder(10);
            entity.Property(x => x.EncryptedAmount).HasColumnOrder(11);

            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PaymentId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PayrollRunId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.EffectiveDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.TypeName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.TypeCode).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.JurisdictionCode).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.EncryptedTaxableWages).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
        }
    }
}
