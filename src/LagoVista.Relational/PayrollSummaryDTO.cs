using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("PayrollSummary", Schema = "dbo")]
    [EncryptionKey("PAYROLLSUMMARY_KEY")]
    public class PayrollSummaryDTO : DbModelBase
    {
        [Required]

        public string EncryptedTotalSalary { get; set; }
        [Required]
        public string EncryptedTotalPayroll { get; set; }
        [Required]
        public string EncryptedTotalExpenses { get; set; }
        [Required]
        public string EncryptedTotalTaxLiability { get; set; }
        [Required]
        public string EncryptedTotalRevenue { get; set; }
        [Required]
        public string EncryptedTaxLiabilities { get; set; }
        [Required]
        public string Status { get; set; }
        public bool Locked { get; set; }
        public DateTime? LockedTimeStamp { get; set; }
        public string LockedByUserId { get; set; }

        [IgnoreOnMapTo]
        [MapTo("LockedByUser")]
        public AppUserDTO LockedUser { get; set; }

        [IgnoreOnMapTo]
        public TimePeriodDTO TimePeriod { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PayrollSummaryDTO>();

            // Relationships
            entity.HasOne(x => x.LockedUser).WithMany().HasForeignKey(x => x.LockedByUserId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.TimePeriod).WithOne(x => x.PayrollSummary).HasForeignKey<TimePeriodDTO>(x => x.PayrollSummaryId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(3);
            entity.Property(x => x.CreationDate).HasColumnOrder(4);
            entity.Property(x => x.LastUpdateDate).HasColumnOrder(5);
            entity.Property(x => x.OrganizationId).HasColumnOrder(6);
            entity.Property(x => x.EncryptedTotalSalary).HasColumnOrder(7);
            entity.Property(x => x.EncryptedTotalPayroll).HasColumnOrder(8);
            entity.Property(x => x.EncryptedTotalExpenses).HasColumnOrder(9);
            entity.Property(x => x.EncryptedTotalTaxLiability).HasColumnOrder(10);
            entity.Property(x => x.EncryptedTotalRevenue).HasColumnOrder(11);
            entity.Property(x => x.EncryptedTaxLiabilities).HasColumnOrder(12);
            entity.Property(x => x.Status).HasColumnOrder(13);
            entity.Property(x => x.Locked).HasColumnOrder(14);
            entity.Property(x => x.LockedTimeStamp).HasColumnOrder(15);
            entity.Property(x => x.LockedByUserId).HasColumnOrder(16);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdateDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.EncryptedTotalSalary).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalPayroll).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalExpenses).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalTaxLiability).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTotalRevenue).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedTaxLiabilities).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.Locked).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.LockedTimeStamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LockedByUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
        }
    }
}