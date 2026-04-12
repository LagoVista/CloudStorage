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
        public string CorrelationId { get; set; }

        [Required]
        public string InvocationKind { get; set; }

        [MapTo("OwnerOrganization")]
        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }


        [Required]
        public string State { get; set; }

        public DateTime RunAt { get; set; }

        public DateTime? ScheduledOccurrence { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }


        public DateTime? ClaimedTime { get; set; }

        public DateTime? ClaimExpires { get; set; }

        public DateTime? StartedTime { get; set; }

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

            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.ManifestId).HasColumnOrder(3);
            entity.Property(x => x.CorrelationId).HasColumnOrder(4);
            entity.Property(x => x.InvocationKind).HasColumnOrder(5);
            entity.Property(x => x.State).HasColumnOrder(6);
            entity.Property(x => x.RunAt).HasColumnOrder(7);
            entity.Property(x => x.ScheduledOccurrence).HasColumnOrder(8);
            entity.Property(x => x.CreationDate).HasColumnOrder(9);
            entity.Property(x => x.ClaimedTime).HasColumnOrder(10);
            entity.Property(x => x.ClaimExpires).HasColumnOrder(11);
            entity.Property(x => x.StartedTime).HasColumnOrder(12);
            entity.Property(x => x.ClaimedByHostId).HasColumnOrder(13);
            entity.Property(x => x.LastExecutionId).HasColumnOrder(14);
            entity.Property(x => x.LastError).HasColumnOrder(15);
            entity.Property(x => x.Version).HasColumnOrder(16);

            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ManifestId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
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
            entity.Property(x => x.Version).HasColumnType(StandardDBTypes.LongStorage(provider));
        }
    }
}