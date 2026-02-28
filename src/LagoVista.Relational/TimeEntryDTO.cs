using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("TimeEntries", Schema = "dbo")]
    public class TimeEntryDTO : DbModelBase
    {
        [Required]
        public Guid AgreementId { get; set; }
        public Guid TimePeriodId { get; set; }
        public Guid? BillingEventId { get; set; }
        public DateTime Date { get; set; }

        [Required]
        public string ProjectId { get; set; }

        [Required]
        public string ProjectName { get; set; }

        [Required]
        public string WorkTaskId { get; set; }

        [Required]
        public string WorkTaskName { get; set; }

        [Required]
        public string UserId { get; set; }
        public bool Locked { get; set; }
        public bool IsEquityTime { get; set; }
        public decimal Hours { get; set; }

        [Required]
        public string Notes { get; set; }
        
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimeEntryDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<TimeEntryDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TimeEntryDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.AgreementId).HasColumnOrder(2);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.TimePeriodId).HasColumnOrder(3);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.BillingEventId).HasColumnOrder(4);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.Date).HasColumnOrder(5);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.OrganizationId).HasColumnOrder(6);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.ProjectId).HasColumnOrder(7);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.ProjectName).HasColumnOrder(8);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.WorkTaskId).HasColumnOrder(9);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.WorkTaskName).HasColumnOrder(10);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.UserId).HasColumnOrder(11);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.Locked).HasColumnOrder(12);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.IsEquityTime).HasColumnOrder(13);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.Hours).HasColumnOrder(14);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.Notes).HasColumnOrder(15);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.CreatedById).HasColumnOrder(16);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(17);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.CreationDate).HasColumnOrder(18);
            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(19);

            modelBuilder.Entity<TimeEntryDTO>().Property(x => x.IsEquityTime).HasDefaultValueSql("0");

            modelBuilder.Entity<TimeEntryDTO>().HasKey(x => new { x.Id });
        }
    }
}
