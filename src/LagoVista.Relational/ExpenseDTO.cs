using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("UserId={id}", IdProperty ="UserId")]
    [Table("Expenses", Schema = "dbo")]
    public class ExpenseDTO : DbModelBase
    {
        public Guid? AgreementId { get; set; }
        [Required]
        public Guid TimePeriodId { get; set; }
        public Guid? BillingEventId { get; set; }

        public Guid? VendorId { get; set; }

        public Guid? PaymentId { get; set; }

        [Required]
        public DateOnly ExpenseDate { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public Guid ExpenseCategoryId { get; set; }
        public string Notes { get; set; }
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string WorkTaskId { get; set; }
        public string WorkTaskName { get; set; }

        public bool Approved { get; set; }
        public string ApprovedById { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [IgnoreOnMapTo]
        public VendorDTO Vendor { get; set; }

        [IgnoreOnMapTo]
        public AgreementDTO Agreement { get; set; }
        [IgnoreOnMapTo]
        public ExpenseCategoryDTO Category { get; set; }
        [IgnoreOnMapTo]
        public PaymentDTO Payment { get; set; }
        [IgnoreOnMapTo]
        public TimePeriodDTO TimePeriod { get; set; }
        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }
        [IgnoreOnMapTo]
        public AppUserDTO ApprovedUser { get; set; }

        public bool Locked { get; set; }

        [Required]
        public string Description { get; set; }
        [Required]
        public string EncryptedAmount { get; set; }
        [Required]
        public string EncryptedReimbursedAmount { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<ExpenseDTO>();

            // Relationships
            entity.HasOne(x => x.Agreement).WithMany().HasForeignKey(x => x.AgreementId);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.ExpenseCategoryId);
            entity.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId);
            entity.HasOne(x => x.TimePeriod).WithMany().HasForeignKey(x => x.TimePeriodId);
            entity.HasOne(x => x.ApprovedUser).WithMany().HasForeignKey(x => x.ApprovedById);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.TimePeriodId).HasColumnOrder(2);
            entity.Property(x => x.ExpenseCategoryId).HasColumnOrder(3);
            entity.Property(x => x.AgreementId).HasColumnOrder(4);
            entity.Property(x => x.BillingEventId).HasColumnOrder(5);
            entity.Property(x => x.PaymentId).HasColumnOrder(6);
            entity.Property(x => x.ExpenseDate).HasColumnOrder(7);
            entity.Property(x => x.ProjectId).HasColumnOrder(8);
            entity.Property(x => x.ProjectName).HasColumnOrder(9);
            entity.Property(x => x.WorkTaskId).HasColumnOrder(10);
            entity.Property(x => x.WorkTaskName).HasColumnOrder(11);
            entity.Property(x => x.UserId).HasColumnOrder(12);
            entity.Property(x => x.OrganizationId).HasColumnOrder(13);
            entity.Property(x => x.Approved).HasColumnOrder(14);
            entity.Property(x => x.ApprovedById).HasColumnOrder(15);
            entity.Property(x => x.ApprovedDate).HasColumnOrder(16);
            entity.Property(x => x.Locked).HasColumnOrder(17);
            entity.Property(x => x.EncryptedAmount).HasColumnOrder(18);
            entity.Property(x => x.EncryptedReimbursedAmount).HasColumnOrder(19);
            entity.Property(x => x.Notes).HasColumnOrder(20);
            entity.Property(x => x.Description).HasColumnOrder(21);
            entity.Property(x => x.CreatedById).HasColumnOrder(22);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(23);
            entity.Property(x => x.CreationDate).HasColumnOrder(24);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(25);
            entity.Property(x => x.VendorId).HasColumnOrder(26);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.TimePeriodId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ExpenseCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.AgreementId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.BillingEventId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PaymentId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.ExpenseDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.ProjectId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ProjectName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.WorkTaskId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.WorkTaskName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Approved).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.ApprovedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ApprovedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.Locked).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.EncryptedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedReimbursedAmount).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.VendorId).HasColumnType(StandardDBTypes.UuidStorage(provider));
        }
    }
}
