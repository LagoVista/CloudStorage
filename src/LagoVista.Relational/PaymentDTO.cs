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
    [Table("Payments", Schema = "dbo")]
    [EncryptionKey("PAYROLL_KEY")]
    public class PaymentDTO : DbModelBase, IEntityHeaderFactory
    {
        public const string PaymentStatus_New = "new";
        public const string PaymentStatus_Approved = "approved";
        public const string PaymentStatus_Funded = "funded";

        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }

        [Required]
        public string UserId { get; set; }

        public Guid TimePeriodId { get; set; }
        [Required]
        public string Status { get; set; }

        public DateOnly? SubmittedDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }

        public decimal BillableHours { get; set; }
        public decimal InternalHours { get; set; }


        [Required]
        public string Gross { get; set; }
        [Required]
        public string Net { get; set; }
        [Required]
        public string Expenses { get; set; }

        public decimal EquityHours { get; set; }
        public string PrimaryTransactionId { get; set; }
        [Required]
        public string PrimaryDeposit { get; set; }
        public string SecondaryTransactionId { get; set; }
        [Required]
        public string EstimatedDeposit { get; set; }


        [Required]
        public string ExpenseDetail { get; set; }
        [Required]
        public string DeductionsDetail { get; set; }
        [Required]
        public string EarnedEquity { get; set; }


        public bool ContractorPayment { get; set; }
        public bool W2Payment { get; set; }
        public bool OfficerPayment { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }

        [IgnoreOnMapTo]
        public TimePeriodDTO TimePeriod { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<PaymentDTO>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.TimePeriod).WithMany().HasForeignKey(x => x.TimePeriodId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Defaults
            entity.Property(x => x.ContractorPayment).HasDefaultValueSql(StandardDbDefaults.True(provider));
            entity.Property(x => x.OfficerPayment).HasDefaultValueSql(StandardDbDefaults.False(provider));
            entity.Property(x => x.W2Payment).HasDefaultValueSql(StandardDbDefaults.False(provider));

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.CreatedById).HasColumnOrder(2);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(3);
            entity.Property(x => x.CreationDate).HasColumnOrder(4);
            entity.Property(x => x.LastUpdateDate).HasColumnOrder(5);
            entity.Property(x => x.UserId).HasColumnOrder(6);
            entity.Property(x => x.TimePeriodId).HasColumnOrder(7);
            entity.Property(x => x.PeriodStart).HasColumnOrder(8);
            entity.Property(x => x.PeriodEnd).HasColumnOrder(9);
            entity.Property(x => x.Status).HasColumnOrder(10);
            entity.Property(x => x.OrganizationId).HasColumnOrder(11);
            entity.Property(x => x.SubmittedDate).HasColumnOrder(12);
            entity.Property(x => x.ExpectedDeliveryDate).HasColumnOrder(13);
            entity.Property(x => x.BillableHours).HasColumnOrder(14);
            entity.Property(x => x.InternalHours).HasColumnOrder(15);
            entity.Property(x => x.EquityHours).HasColumnOrder(16);
            entity.Property(x => x.Gross).HasColumnOrder(17);
            entity.Property(x => x.Net).HasColumnOrder(18);
            entity.Property(x => x.Expenses).HasColumnOrder(19);
            entity.Property(x => x.PrimaryTransactionId).HasColumnOrder(20);
            entity.Property(x => x.SecondaryTransactionId).HasColumnOrder(21);
            entity.Property(x => x.PrimaryDeposit).HasColumnOrder(22);
            entity.Property(x => x.EstimatedDeposit).HasColumnOrder(23);
            entity.Property(x => x.ExpenseDetail).HasColumnOrder(24);
            entity.Property(x => x.DeductionsDetail).HasColumnOrder(25);
            entity.Property(x => x.EarnedEquity).HasColumnOrder(26);
            entity.Property(x => x.ContractorPayment).HasColumnOrder(27);
            entity.Property(x => x.W2Payment).HasColumnOrder(28);
            entity.Property(x => x.OfficerPayment).HasColumnOrder(29);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdateDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.TimePeriodId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.PeriodStart).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.PeriodEnd).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.Status).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.SubmittedDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.ExpectedDeliveryDate).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.BillableHours).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.InternalHours).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.EquityHours).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.Gross).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.Net).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.Expenses).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.PrimaryTransactionId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.SecondaryTransactionId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.PrimaryDeposit).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.EstimatedDeposit).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.ExpenseDetail).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.DeductionsDetail).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.EarnedEquity).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.ContractorPayment).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.W2Payment).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.OfficerPayment).HasColumnType(StandardDBTypes.FlagStorage(provider));
        }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), $"{User?.FullName} - {PeriodStart:MM/dd/yyyy} to {PeriodEnd:MM/dd/yyyy}");
        }
    }
}