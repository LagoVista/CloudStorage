using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("org-{id}", IdPath = "OrganizationId")]
    [Table("PayrollRun", Schema = "dbo")]
    [EncryptionKey("PAYROLLSUMMARY_KEY")]
    public class PayrollRunDTO : DbModelBase
    {
        [Required]

        public string EncryptedTotalSalary { get; set; }
        [Required]
        public string EncryptedTotalGross { get; set; }
        [Required]
        public string EncryptedTotalNet { get; set; }
        [Required]
        public string EncryptedTotalPayroll { get; set; }
        [Required]
        public string EncryptedTotalExpenses { get; set; }
        [Required]
        public string EncryptedTotalPayrollTaxObligation { get; set; }
        [Required]
        public string EncryptedTotalRevenue { get; set; }

        [IgnoreOnMapTo]
        public string EncryptedTaxLiabilities { get; set; }
        [Required]
        public string Status { get; set; }
        public bool Locked { get; set; }
        public DateTime? LockedTimestamp { get; set; }
        public string LockedByUserId { get; set; }

        public bool Approved { get; set; }

        public DateTime? ApprovedTimestamp { get; set; } 

        public string ApprovedByUserId { get; set; }

        [IgnoreOnMapTo]
        [MapTo("LockedByUser")]
        public AppUserDTO LockedByUser { get; set; }

        [IgnoreOnMapTo]
        [MapTo("ApprovedByUser")]
        public AppUserDTO ApprovedByUser { get; set; }


        [IgnoreOnMapTo]
        public TimePeriodDTO TimePeriod { get; set; }

        [IgnoreOnMapTo]
        public List<PayrollRunDeductionDTO> Deductions { get; set; }

        [IgnoreOnMapTo]
        public List<PaymentDTO> Payments { get; set; }

        [IgnoreOnMapTo]
        public List<PaymentEmployerTaxDetailDTO> PayrollTaxDetails { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PayrollRunDTO>();

            // Relationships
            entity.HasOne(x => x.LockedByUser).WithMany().HasForeignKey(x => x.LockedByUserId);
            entity.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.TimePeriod).WithOne(x => x.PayrollRun).HasForeignKey<TimePeriodDTO>(x => x.PayrollRunId);
            entity.HasMany(x => x.Payments).WithOne(x => x.PayrollRun).HasForeignKey(x => x.PayrollRunId);
            entity.HasMany(x => x.Deductions).WithOne(x => x.PayrollRun).HasForeignKey(x => x.PayrollRunId);    
            entity.HasMany(x => x.PayrollTaxDetails).WithOne(x => x.PayrollRun).HasForeignKey(x => x.PayrollRunId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(3);
            entity.Property(x => x.CreationDate).HasColumnOrder(4);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(5);
            entity.Property(x => x.OrganizationId).HasColumnOrder(6);
            entity.Property(x => x.EncryptedTotalSalary).HasColumnOrder(7);
            entity.Property(x => x.EncryptedTotalGross).HasColumnOrder(8);
            entity.Property(x => x.EncryptedTotalNet).HasColumnOrder(9);
            entity.Property(x => x.EncryptedTotalPayroll).HasColumnOrder(10);
            entity.Property(x => x.EncryptedTotalExpenses).HasColumnOrder(11);
            entity.Property(x => x.EncryptedTotalPayrollTaxObligation).HasColumnOrder(12);
            entity.Property(x => x.EncryptedTotalRevenue).HasColumnOrder(13);
            entity.Property(x => x.EncryptedTaxLiabilities).HasColumnOrder(14);
            entity.Property(x => x.Status).HasColumnOrder(15);
            entity.Property(x => x.Locked).HasColumnOrder(16);
            entity.Property(x => x.LockedTimestamp).HasColumnOrder(17);
            entity.Property(x => x.LockedByUserId).HasColumnOrder(18);
            entity.Property(x => x.Approved).HasColumnOrder(19);
            entity.Property(x => x.ApprovedTimestamp).HasColumnOrder(20);
            entity.Property(x => x.ApprovedByUserId).HasColumnOrder(21);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.EncryptedTotalSalary).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalPayroll).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalGross).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalNet).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalExpenses).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalPayrollTaxObligation).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalRevenue).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTaxLiabilities).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.Locked).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.LockedTimestamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LockedByUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Approved).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.ApprovedTimestamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.ApprovedByUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
        }
    }
}