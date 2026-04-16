// src/LagoVista.Relational/JobInvocationRequestDTO.cs
using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("JobInvocationRequests", Schema = "dbo")]
    public class JobInvocationRequestDTO
    {
        [Key]
        public Guid Id { get; set; }

        [MapFrom("OwnerOrganization")]
        [Required]
        public string OrganizationId { get; set; }

        [Required]
        public string ManifestId { get; set; }

        [Required]
        public string ManifestName { get; set; }

        public string ScheduleEntryId { get; set; }

        public string ScheduleEntryName { get; set; }

        public bool IsEnabled { get; set; }

        [Required]
        public string CorrelationId { get; set; }

        [Required]
        public string InvocationKind { get; set; }

        [MapTo("OwnerOrganization")]
        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }


        [Required]
        public string State { get; set; }

        public DateTime? RunAt { get; set; }

        public DateTime? ScheduledOccurrence { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }


        public DateTime? ClaimedTime { get; set; }

        public DateTime? ClaimExpires { get; set; }

        public DateTime? StartedTime { get; set; }

        public DateTime? BlockedTime { get; set; }

        public string BlockedReason { get; set; }

        public string ClaimedByHostId { get; set; }

        public string LastExecutionId { get; set; }
        public string LastError { get; set; }


        [IgnoreOnMapTo()]
        public long Version { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<JobInvocationRequestDTO>();

            entity.ToTable("JobInvocationRequests", "dbo");

            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => new { x.State, x.RunAt });
            entity.HasIndex(x => new { x.State, x.ClaimExpires });
            entity.HasIndex(x => x.ManifestId);
            entity.HasIndex(x => x.OrganizationId);

            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ManifestId).HasColumnOrder(3);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ManifestName).HasColumnOrder(4);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ScheduleEntryId).HasColumnOrder(5);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ScheduleEntryName).HasColumnOrder(6);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.IsEnabled).HasColumnOrder(7);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.CorrelationId).HasColumnOrder(8);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.InvocationKind).HasColumnOrder(9);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.State).HasColumnOrder(10);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.RunAt).HasColumnOrder(11);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ScheduledOccurrence).HasColumnOrder(12);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.CreationDate).HasColumnOrder(13);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ClaimedTime).HasColumnOrder(14);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ClaimExpires).HasColumnOrder(15);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.StartedTime).HasColumnOrder(16);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.ClaimedByHostId).HasColumnOrder(17);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.LastExecutionId).HasColumnOrder(18);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.LastError).HasColumnOrder(19);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.BlockedTime).HasColumnOrder(20);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.BlockedReason).HasColumnOrder(21);
            modelBuilder.Entity<JobInvocationRequestDTO>().Property(x => x.Version).HasColumnOrder(22);


            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ManifestId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ManifestName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.ScheduleEntryId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ScheduleEntryName).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.CorrelationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.InvocationKind).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.State).HasColumnType(StandardDBTypes.CategoryStorage(provider));
            entity.Property(x => x.RunAt).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.ScheduledOccurrence).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.ClaimedTime).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.ClaimExpires).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.StartedTime).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.ClaimedByHostId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastExecutionId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastError).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.BlockedTime).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.BlockedReason).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.Version).HasColumnType(StandardDBTypes.LongStorage(provider));
        }
    }
}