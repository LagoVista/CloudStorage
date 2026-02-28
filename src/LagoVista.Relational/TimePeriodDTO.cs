using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
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
        public static void Configure(ModelBuilder modelBuilder)
        {
    

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
        }
    }

}
