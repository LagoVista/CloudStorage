using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [ModernKeyId("user-{id}", IdPath = "UserId", CreateIfMissing =true)]
    [Table("Payments", Schema = "dbo")]
    [EncryptionKey("PAYROLL_KEY")]
    public class PaymentDTO : DbModelBase, IEntityHeaderFactory
    {
        public const string PaymentStatus_New = "new";
        public const string PaymentStatus_Approved = "approved";
        public const string PaymentStatus_Funded = "funded";

        public Guid PayrollRunId { get; set; }

        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }

        [Required]
        public string UserId { get; set; }

        public Guid TimePeriodId { get; set; }
        [Required]
        public string PaymentStatus { get; set; }

        public DateOnly? SubmittedDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }

        public decimal BillableHours { get; set; }
        public decimal InternalHours { get; set; }


        [Required]
        public string EncryptedGross { get; set; }
        [Required]
        public string EncryptedNet { get; set; }
        [Required]
        public string EncryptedExpenses { get; set; }

        public decimal EquityHours { get; set; }
        public string PrimaryTransactionId { get; set; }
        [Required]
        public string EncryptedPrimaryDeposit { get; set; }
        public string SecondaryTransactionId { get; set; }
        [Required]
        public string EncryptedEstimatedTaxWithholding { get; set; }

        public string EncryptedSecondaryDeposit { get; set; }

        [Required]
        public string EncryptedEarnedEquity { get; set; }

        [IgnoreOnMapTo]
        public string ExpenseDetail { get; set; } = "legacy";

        [IgnoreOnMapTo]
        public string DeductionsDetail { get; set; } = "legacy";

        public bool ContractorPayment { get; set; }
        public bool W2Payment { get; set; }
        public bool OfficerPayment { get; set; }

        [IgnoreOnMapTo]
        public List<ExpenseReimbursementDTO>  ExpenseReimbursements { get; set; }

        [IgnoreOnMapTo]
        public List<PaymentDeductionDTO> Deductions { get; set; }

        [IgnoreOnMapTo]
        public List<PaymentEmployerTaxDetailDTO> PayrollTaxDetails { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }

        [IgnoreOnMapTo]
        public TimePeriodDTO TimePeriod { get; set; }

        [IgnoreOnMapTo]
        public PayrollRunDTO PayrollRun { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PaymentDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.TimePeriod).WithMany().HasForeignKey(x => x.TimePeriodId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.HasMany(x => x.Deductions).WithOne(x => x.Payment).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.ExpenseReimbursements).WithOne(x => x.Payment).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.PayrollTaxDetails).WithOne(x => x.Payment).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(3);
            entity.Property(x => x.CreationDate).HasColumnOrder(4);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(5);
            entity.Property(x => x.UserId).HasColumnOrder(6);
            entity.Property(x => x.TimePeriodId).HasColumnOrder(7);
            entity.Property(x => x.PayrollRunId).HasColumnOrder(8);
            entity.Property(x => x.PeriodStart).HasColumnOrder(9);
            entity.Property(x => x.PeriodEnd).HasColumnOrder(10);
            entity.Property(x => x.PaymentStatus).HasColumnOrder(11);
            entity.Property(x => x.OrganizationId).HasColumnOrder(12);
            entity.Property(x => x.SubmittedDate).HasColumnOrder(13);
            entity.Property(x => x.ExpectedDeliveryDate).HasColumnOrder(14);
            entity.Property(x => x.BillableHours).HasColumnOrder(15);
            entity.Property(x => x.InternalHours).HasColumnOrder(16);
            entity.Property(x => x.EquityHours).HasColumnOrder(17);
            entity.Property(x => x.EncryptedGross).HasColumnOrder(18);
            entity.Property(x => x.EncryptedNet).HasColumnOrder(19);
            entity.Property(x => x.EncryptedExpenses).HasColumnOrder(20);
            entity.Property(x => x.PrimaryTransactionId).HasColumnOrder(21);
            entity.Property(x => x.SecondaryTransactionId).HasColumnOrder(22);
            entity.Property(x => x.EncryptedPrimaryDeposit).HasColumnOrder(23);
            entity.Property(x => x.EncryptedEstimatedTaxWithholding).HasColumnOrder(24);
            entity.Property(x => x.ExpenseDetail).HasColumnOrder(25);
            entity.Property(x => x.DeductionsDetail).HasColumnOrder(26);
            entity.Property(x => x.EncryptedEarnedEquity).HasColumnOrder(27);
            entity.Property(x => x.ContractorPayment).HasColumnOrder(28);
            entity.Property(x => x.W2Payment).HasColumnOrder(29);
            entity.Property(x => x.OfficerPayment).HasColumnOrder(30);
            entity.Property(x => x.EncryptedSecondaryDeposit).HasColumnOrder(31);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.TimePeriodId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PayrollRunId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PeriodStart).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.PeriodEnd).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.PaymentStatus).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.SubmittedDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.ExpectedDeliveryDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.BillableHours).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.InternalHours).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.EquityHours).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.EncryptedGross).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedNet).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedExpenses).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.PrimaryTransactionId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.SecondaryTransactionId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.EncryptedPrimaryDeposit).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.EncryptedEstimatedTaxWithholding).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.ExpenseDetail).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.DeductionsDetail).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.EncryptedEarnedEquity).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
            entity.Property(x => x.ContractorPayment).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.W2Payment).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.OfficerPayment).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.EncryptedSecondaryDeposit).HasColumnType(StandardDBTypes.EncryptionStorage(provider));
        }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), $"{User?.FullName} - {PeriodStart:MM/dd/yyyy} to {PeriodEnd:MM/dd/yyyy}");
        }
    }
}