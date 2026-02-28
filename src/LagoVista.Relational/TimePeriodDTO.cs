using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("TimePeriods", Schema = "dbo")]
    public class TimePeriodDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string OrganizationId { get; set; }

        public int Year { get; set; }
        public DateOnly Start { get; set; }
        public DateOnly End { get; set; }
        public bool Locked { get; set; }

        public Guid? PayrollSummaryId { get; set; }

        [IgnoreOnMapTo]
        public PayrollSummaryDTO PayrollSummary { get; set; }

        public DateTime? LockedTimeStamp { get; set; }
        public string LockedByUserId { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO LockedByUser { get; set; }

        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }



        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimePeriodDTO>()
            .HasOne(tp => tp.LockedByUser)
            .WithMany()
            .HasForeignKey(tp => tp.LockedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TimePeriodDTO>()
            .HasOne(tp => tp.Organization)
            .WithMany()
            .HasForeignKey(tp => tp.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<TimePeriodDTO>()
            .HasOne(tp => tp.PayrollSummary)    
            .WithOne(ps => ps.TimePeriod)
            .HasForeignKey<TimePeriodDTO>(tp => tp.PayrollSummaryId);

            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.Year).HasColumnOrder(2);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.OrganizationId).HasColumnOrder(3);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.Locked).HasColumnOrder(4);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.LockedByUserId).HasColumnOrder(5);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.LockedTimeStamp).HasColumnOrder(6);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.PayrollSummaryId).HasColumnOrder(7);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.Start).HasColumnOrder(8);
            modelBuilder.Entity<TimePeriodDTO>().Property(x => x.End).HasColumnOrder(9);

            modelBuilder.Entity<TimePeriodDTO>().HasKey(x => new { x.Id });
        }
    }
}
