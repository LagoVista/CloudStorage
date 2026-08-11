using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("ArtifactInformationElements", Schema = "dbo")]
    public class ArtifactInformationElementDTO
    {
        public string ArtifactId { get; set; }

        public string DefinitionId { get; set; }

        public string OwnerOrganizationId { get; set; }

        public string UsageRole { get; set; }

        public System.DateTime CreationDate { get; set; }

        public System.DateTime LastUpdatedDate { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var provider = modelBuilder.GetProviderName();
            var entity = modelBuilder.Entity<ArtifactInformationElementDTO>();

            entity.HasKey(x => new { x.ArtifactId, x.DefinitionId });
            entity.HasIndex(x => x.OwnerOrganizationId).HasDatabaseName("IX_ArtifactInformationElements_OwnerOrganizationId");
            entity.HasIndex(x => x.DefinitionId).HasDatabaseName("IX_ArtifactInformationElements_DefinitionId");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.ArtifactId }).HasDatabaseName("IX_ArtifactInformationElements_Organization_ArtifactId");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.DefinitionId }).HasDatabaseName("IX_ArtifactInformationElements_Organization_DefinitionId");

            entity.HasOne<ArtifactDTO>().WithMany().HasForeignKey(x => x.ArtifactId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<DefinitionDTO>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.ArtifactId).HasColumnOrder(1).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.DefinitionId).HasColumnOrder(2).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.OwnerOrganizationId).HasColumnOrder(3).HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
            entity.Property(x => x.UsageRole).HasColumnOrder(4).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.CreationDate).HasColumnOrder(5).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(6).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}
