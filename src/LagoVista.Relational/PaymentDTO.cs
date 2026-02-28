using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Payments", Schema = "dbo")]
    [EncryptionKey("PAYROLL_KEY")]
    public class PaymentDTO : DbModelBase
    {
        public const string PaymentStatus_New = "new";
        public const string PaymentStatus_Approved = "approved";
        public const string PaymentStatus_Funded = "funded";

        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }

        [IgnoreOnMapTo]
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
        public bool OfficierPayment { get; set; }

        private AppUserDTO _user;
        public AppUserDTO User
        {
            get => _user;
            set
            {
                _user = value;
                UserId = _user?.AppUserId;
            }
        }

        [IgnoreOnMapTo]
        public TimePeriodDTO TimePeriod { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentDTO>()
             .HasOne(ps => ps.Organization)
             .WithMany()
             .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<PaymentDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PaymentDTO>()
            .HasOne(ps => ps.TimePeriod)
            .WithMany()
            .HasForeignKey(ps => ps.TimePeriodId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PaymentDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<PaymentDTO>()
            .HasOne(py => py.User)
            .WithMany()
            .HasForeignKey(tp => tp.UserId);

            modelBuilder.Entity<PaymentDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.CreatedById).HasColumnOrder(2);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(3);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.CreationDate).HasColumnOrder(4);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(5);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.UserId).HasColumnOrder(6);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.TimePeriodId).HasColumnOrder(7);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.PeriodStart).HasColumnOrder(8);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.PeriodEnd).HasColumnOrder(9);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.Status).HasColumnOrder(10);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.OrganizationId).HasColumnOrder(11);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.SubmittedDate).HasColumnOrder(12);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.ExpectedDeliveryDate).HasColumnOrder(13);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.BillableHours).HasColumnOrder(14);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.InternalHours).HasColumnOrder(15);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.EquityHours).HasColumnOrder(16);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.Gross).HasColumnOrder(17);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.Net).HasColumnOrder(18);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.Expenses).HasColumnOrder(19);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.PrimaryTransactionId).HasColumnOrder(20);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.SecondaryTransactionId).HasColumnOrder(21);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.PrimaryDeposit).HasColumnOrder(22);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.EstimatedDeposit).HasColumnOrder(23);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.ExpenseDetail).HasColumnOrder(24);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.DeductionsDetail).HasColumnOrder(25);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.EarnedEquity).HasColumnOrder(26);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.ContractorPayment).HasColumnOrder(27);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.W2Payment).HasColumnOrder(28);
            modelBuilder.Entity<PaymentDTO>().Property(x => x.OfficierPayment).HasColumnOrder(29);

            modelBuilder.Entity<PaymentDTO>().Property(x => x.ContractorPayment).HasDefaultValueSql("1");
            modelBuilder.Entity<PaymentDTO>().Property(x => x.OfficierPayment).HasDefaultValueSql("0");
            modelBuilder.Entity<PaymentDTO>().Property(x => x.W2Payment).HasDefaultValueSql("0");
        }
    }
}
