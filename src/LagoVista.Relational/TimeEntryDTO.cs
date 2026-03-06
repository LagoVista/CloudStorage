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
        public DateOnly Date { get; set; }

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
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<TimeEntryDTO>();

            // Relationships
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.AgreementId).HasColumnOrder(2);
            entity.Property(x => x.TimePeriodId).HasColumnOrder(3);
            entity.Property(x => x.BillingEventId).HasColumnOrder(4);
            entity.Property(x => x.Date).HasColumnOrder(5);
            entity.Property(x => x.OrganizationId).HasColumnOrder(6);
            entity.Property(x => x.ProjectId).HasColumnOrder(7);
            entity.Property(x => x.ProjectName).HasColumnOrder(8);
            entity.Property(x => x.WorkTaskId).HasColumnOrder(9);
            entity.Property(x => x.WorkTaskName).HasColumnOrder(10);
            entity.Property(x => x.UserId).HasColumnOrder(11);
            entity.Property(x => x.Locked).HasColumnOrder(12);
            entity.Property(x => x.IsEquityTime).HasColumnOrder(13);
            entity.Property(x => x.Hours).HasColumnOrder(14);
            entity.Property(x => x.Notes).HasColumnOrder(15);
            entity.Property(x => x.CreatedById).HasColumnOrder(16);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(17);
            entity.Property(x => x.CreationDate).HasColumnOrder(18);
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(19);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.AgreementId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.TimePeriodId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.BillingEventId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.Date).HasColumnType(StandardDBTypes.CalendarDateStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ProjectId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ProjectName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.WorkTaskId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.WorkTaskName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.UserId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Locked).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.IsEquityTime).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Hours).HasColumnType(StandardDBTypes.DecimalSmall(provider));
            entity.Property(x => x.Notes).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}