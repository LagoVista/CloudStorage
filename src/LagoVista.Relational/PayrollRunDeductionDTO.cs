using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("org-{id}", IdPath = "OrganizationId")]
    [Table("PayrollRunDeduction", Schema = "dbo")]
    [EncryptionKey("Org-{id}", IdProperty = nameof(PayrollRunDeductionDTO.OrganizationId), CreateIfMissing = true)]
    public class PayrollRunDeductionDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PayrollRunId { get; set; }

        [Required]
        public string OrganizationId { get; set; }

        [Required]
        public DateOnly PayrollDate { get; set; }

        [Required]
        public string TypeName { get; set; }
        [Required]
        public string TypeCode { get; set; }

        [Required]
        public string EncryptedAmount { get; set; }

        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }

        [IgnoreOnMapTo]
        public PayrollRunDTO PayrollRun { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PayrollRunDeductionDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PayrollRun).WithMany(x => x.Deductions).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade).OnDelete(DeleteBehavior.Cascade);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.PayrollRunId).HasColumnOrder(2);
            entity.Property(x => x.OrganizationId).HasColumnOrder(3);
            entity.Property(x => x.PayrollDate).HasColumnOrder(4);
            entity.Property(x => x.TypeName).HasColumnOrder(5);
            entity.Property(x => x.TypeCode).HasColumnOrder(6);
            entity.Property(x => x.EncryptedAmount).HasColumnOrder(7);

            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PayrollRunId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.PayrollDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.TypeName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.TypeCode).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.EncryptedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
        }
    }
}
