using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Embeddings", Schema = "dbo")]
    public class EmbeddingDTO
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string OwnerOrganizationId { get; set; }

        [Required]
        public string DefinitionId { get; set; }

        [Required]
        public string ModelKey { get; set; }

        public int Dimensions { get; set; }

        [Required]
        public string SourceSha256 { get; set; }

        [Required]
        public byte[] Vector { get; set; }

        public DateTime GeneratedUtc { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var provider = modelBuilder.GetProviderName();
            var entity = modelBuilder.Entity<EmbeddingDTO>();

            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OwnerOrganizationId).HasDatabaseName("IX_Embeddings_OwnerOrganizationId");
            entity.HasIndex(x => new { x.DefinitionId, x.ModelKey }).IsUnique().HasDatabaseName("UX_Embeddings_DefinitionId_ModelKey");
            entity.HasIndex(x => x.SourceSha256).HasDatabaseName("IX_Embeddings_SourceSha256");

            entity.HasOne<DefinitionDTO>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Cascade);

            entity.Property(x => x.Id).HasColumnOrder(1).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OwnerOrganizationId).HasColumnOrder(2).HasColumnType("varchar(32)").HasMaxLength(32);
            entity.Property(x => x.DefinitionId).HasColumnOrder(3).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ModelKey).HasColumnOrder(4).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Dimensions).HasColumnOrder(5).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.SourceSha256).HasColumnOrder(6).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Vector).HasColumnOrder(7).HasColumnType(provider switch
            {
                ModelBuilderProviderExtensions.Sqlite => "BLOB",
                ModelBuilderProviderExtensions.Postgres => "bytea",
                _ => "varbinary(max)"
            });
            entity.Property(x => x.GeneratedUtc).HasColumnOrder(8).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}
