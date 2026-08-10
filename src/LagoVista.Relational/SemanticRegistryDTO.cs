using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Registry", Schema = "dbo")]
    public class SemanticRegistryDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string OwnerOrganizationId { get; set; }

        [Required]
        public string Name { get; set; }

        public long Revision { get; set; }

        public long? SeededFromRevision { get; set; }

        public DateTime? SeededUtc { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime UpdatedUtc { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var provider = modelBuilder.GetProviderName();
            var entity = modelBuilder.Entity<SemanticRegistryDTO>();

            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OwnerOrganizationId).HasDatabaseName("IX_Registry_OwnerOrganizationId");
            entity.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_Registry_Name");

            entity.Property(x => x.Id).HasColumnOrder(1).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OwnerOrganizationId).HasColumnOrder(2).HasColumnType("varchar(32)").HasMaxLength(32);
            entity.Property(x => x.Name).HasColumnOrder(3).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Revision).HasColumnOrder(4).HasColumnType(StandardDBTypes.LongStorage(provider));
            entity.Property(x => x.SeededFromRevision).HasColumnOrder(5).HasColumnType(StandardDBTypes.LongStorage(provider));
            entity.Property(x => x.SeededUtc).HasColumnOrder(6).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.CreatedUtc).HasColumnOrder(7).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.UpdatedUtc).HasColumnOrder(8).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}
