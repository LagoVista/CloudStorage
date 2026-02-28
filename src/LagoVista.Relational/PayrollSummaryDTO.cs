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
            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ps => ps.LockedUser)
            .WithMany()
            .HasForeignKey(ps => ps.LockedByUserId);

            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ex => ex.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId);


            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ex => ex.TimePeriod)
            .WithOne(tp => tp.PayrollSummary)
            .HasForeignKey<TimePeriodDTO>(tp => tp.PayrollSummaryId);

            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.CreatedById).HasColumnOrder(2);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(3);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.CreationDate).HasColumnOrder(4);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(5);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.OrganizationId).HasColumnOrder(6);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.EncryptedTotalSalary).HasColumnOrder(7);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.EncryptedTotalPayroll).HasColumnOrder(8);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.EncryptedTotalExpenses).HasColumnOrder(9);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.EncryptedTotalTaxLiability).HasColumnOrder(10);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.EncryptedTotalRevenue).HasColumnOrder(11);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.EncryptedTaxLiabilities).HasColumnOrder(12);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.Status).HasColumnOrder(13);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.Locked).HasColumnOrder(14);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.LockedTimeStamp).HasColumnOrder(15);
            modelBuilder.Entity<PayrollSummaryDTO>().Property(x => x.LockedByUserId).HasColumnOrder(16);

            modelBuilder.Entity<PayrollSummaryDTO>().HasKey(x => new { x.Id });
        }
    }
}