using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("user-{id}", IdPath = "UserId")]
    [Table("PayRates", Schema = "dbo")]
    [EncryptionKey("RateKey-{id}", IdProperty = nameof(PayRateDTO.UserId), CreateIfMissing = true)]
    public class PayRateDTO : DbModelBase, IEntityHeaderFactory
    {
        [Required]
        public string UserId { get; set; }
        public Guid? WorkRoleId { get; set; }
        public DateOnly Start { get; set; }
        public DateOnly? End { get; set; }
        [Required]
        public string Notes { get; set; }

        public bool IsFTE { get; set; }
        public bool IsContractor { get; set; }
        public bool IsOfficer { get; set; }


        public bool IsSalary { get; set; }
        public bool DeductEstimated { get; set; }
        public decimal DeductEstimatedRate { get; set; }
        public string EncryptedSalary { get; set; }
        public string EncryptedDeductions { get; set; }

        public string EncryptedEquityScaler { get; set; }
        public string EncryptedBillableRate { get; set; }
        public string EncryptedInternalRate { get; set; }
        public string FilingType { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }

        [IgnoreOnMapTo]
        public WorkRoleDTO WorkRole { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PayRateDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkRole).WithMany().HasForeignKey(x => x.WorkRoleId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.UserId).HasColumnOrder(3);
            entity.Property(x => x.Start).HasColumnOrder(4);
            entity.Property(x => x.End).HasColumnOrder(5);
            entity.Property(x => x.IsSalary).HasColumnOrder(6);
            entity.Property(x => x.FilingType).HasColumnOrder(7);
            entity.Property(x => x.DeductEstimated).HasColumnOrder(8);
            entity.Property(x => x.DeductEstimatedRate).HasColumnOrder(9);
            entity.Property(x => x.EncryptedBillableRate).HasColumnOrder(10);
            entity.Property(x => x.EncryptedInternalRate).HasColumnOrder(11);
            entity.Property(x => x.EncryptedSalary).HasColumnOrder(12);
            entity.Property(x => x.EncryptedDeductions).HasColumnOrder(13);
            entity.Property(x => x.EncryptedEquityScaler).HasColumnOrder(14);
            entity.Property(x => x.Notes).HasColumnOrder(15);
            entity.Property(x => x.CreatedById).HasColumnOrder(16);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(17);
            entity.Property(x => x.CreationDate).HasColumnOrder(18);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(19);
            entity.Property(x => x.WorkRoleId).HasColumnOrder(20);
            entity.Property(x => x.IsContractor).HasColumnOrder(21);
            entity.Property(x => x.IsFTE).HasColumnOrder(22);
            entity.Property(x => x.IsOfficer).HasColumnOrder(23);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Start).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.End).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.IsSalary).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.FilingType).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.DeductEstimated).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.DeductEstimatedRate).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.EncryptedBillableRate).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedInternalRate).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedSalary).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedDeductions).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedEquityScaler).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.WorkRoleId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.IsContractor).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.IsFTE).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.IsOfficer).HasColumnType(StandardDBTypes.FlagStorage(provider));
        }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), $"{User.FullName} {Start}" );
        }
    }
}