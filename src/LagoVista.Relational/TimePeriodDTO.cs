using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("TimePeriods", Schema = "dbo")]
    public class TimePeriodDTO : IEntityHeaderFactory
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

        public DateTime? LockedTimestamp { get; set; }
        public string LockedByUserId { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO LockedByUser { get; set; }

        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }

        [IgnoreOnMapTo]
        public List<TimeEntryDTO> TimeEntries { get; set; }



        public EntityHeader ToEntityHeader() => EntityHeader.Create(Id.ToString(), $"{Start} to {End}");
        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<TimePeriodDTO>();

            // Relationships
            entity.HasOne(x => x.LockedByUser).WithMany().HasForeignKey(x => x.LockedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PayrollSummary).WithOne(x => x.TimePeriod).HasForeignKey<TimePeriodDTO>(x => x.PayrollSummaryId);
            entity.HasMany(x => x.TimeEntries).WithOne(x => x.TimePeriod).HasForeignKey(x => x.TimePeriodId).OnDelete(DeleteBehavior.Restrict);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.Year).HasColumnOrder(2);
            entity.Property(x => x.OrganizationId).HasColumnOrder(3);
            entity.Property(x => x.Locked).HasColumnOrder(4);
            entity.Property(x => x.LockedByUserId).HasColumnOrder(5);
            entity.Property(x => x.LockedTimestamp).HasColumnOrder(6);
            entity.Property(x => x.PayrollSummaryId).HasColumnOrder(7);
            entity.Property(x => x.Start).HasColumnOrder(8);
            entity.Property(x => x.End).HasColumnOrder(9);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Year).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Locked).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.LockedByUserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LockedTimestamp).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.PayrollSummaryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Start).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.End).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
        }
    }
}
