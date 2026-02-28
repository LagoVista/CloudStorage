using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("PayRates" , Schema = "dbo")]
    [EncryptionKey("Rate-{id}", IdProperty = nameof(PayRateDTO.UserId), CreateIfMissing = true)]
    public class PayRateDTO : DbModelBase
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
        public bool IsOfficier { get; set; }


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
            modelBuilder.Entity<PayRateDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<PayRateDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PayRateDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);



            modelBuilder.Entity<PayRateDTO>()
            .HasOne(ps => ps.User)
            .WithMany()
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PayRateDTO>()
            .HasOne(ps => ps.WorkRole)
            .WithMany()
            .HasForeignKey(ps => ps.WorkRoleId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PayRateDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.UserId).HasColumnOrder(3);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.Start).HasColumnOrder(4);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.End).HasColumnOrder(5);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsSalary).HasColumnOrder(6);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.FilingType).HasColumnOrder(7);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.DeductEstimated).HasColumnOrder(8);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.DeductEstimatedRate).HasColumnOrder(9);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.EncryptedBillableRate).HasColumnOrder(10);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.EncryptedInternalRate).HasColumnOrder(11);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.EncryptedSalary).HasColumnOrder(12);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.EncryptedDeductions).HasColumnOrder(13);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.EncryptedEquityScaler).HasColumnOrder(14);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.Notes).HasColumnOrder(15);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.CreatedById).HasColumnOrder(16);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(17);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.CreationDate).HasColumnOrder(18);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(19);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.WorkRoleId).HasColumnOrder(20);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsContractor).HasColumnOrder(21);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsFTE).HasColumnOrder(22);
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsOfficier).HasColumnOrder(23);

            modelBuilder.Entity<PayRateDTO>().Property(x => x.DeductEstimated).HasDefaultValueSql("0");
            modelBuilder.Entity<PayRateDTO>().Property(x => x.DeductEstimatedRate).HasDefaultValueSql("0");
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsContractor).HasDefaultValueSql("1");
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsFTE).HasDefaultValueSql("0");
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsOfficier).HasDefaultValueSql("0");
            modelBuilder.Entity<PayRateDTO>().Property(x => x.IsSalary).HasDefaultValueSql("0");

            modelBuilder.Entity<PayRateDTO>().HasKey(x => new { x.Id });
        }
    }
}
