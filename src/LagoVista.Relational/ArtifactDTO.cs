using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Artifacts", Schema = "dbo")]
    public class ArtifactDTO
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string OwnerOrganizationId { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Tla { get; set; }

        public string Description { get; set; }

        public string PurposeSummary { get; set; }

        [Required]
        public string StatusKey { get; set; }

        [Required]
        public string ScopeTypeKey { get; set; }

        [Required]
        public string ArchetypeKey { get; set; }

        [Required]
        public string ProductionCardinalityKey { get; set; }

        [Required]
        public string SpecificationSha256 { get; set; }

        public System.DateTime CreationDate { get; set; }

        public System.DateTime LastUpdatedDate { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var provider = modelBuilder.GetProviderName();
            var entity = modelBuilder.Entity<ArtifactDTO>();

            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OwnerOrganizationId).HasDatabaseName("IX_Artifacts_OwnerOrganizationId");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.Key }).IsUnique().HasDatabaseName("UX_Artifacts_Organization_Key");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.Tla }).IsUnique().HasDatabaseName("UX_Artifacts_Organization_Tla");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.StatusKey }).HasDatabaseName("IX_Artifacts_Organization_StatusKey");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.ScopeTypeKey }).HasDatabaseName("IX_Artifacts_Organization_ScopeTypeKey");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.ArchetypeKey }).HasDatabaseName("IX_Artifacts_Organization_ArchetypeKey");
            entity.HasIndex(x => new { x.OwnerOrganizationId, x.ProductionCardinalityKey }).HasDatabaseName("IX_Artifacts_Organization_ProductionCardinalityKey");

            entity.Property(x => x.Id).HasColumnOrder(1).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.OwnerOrganizationId).HasColumnOrder(2).HasColumnType("varchar(32)").HasMaxLength(32);
            entity.Property(x => x.Key).HasColumnOrder(3).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Name).HasColumnOrder(4).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Tla).HasColumnOrder(5).HasColumnType("varchar(16)").HasMaxLength(16);
            entity.Property(x => x.Description).HasColumnOrder(6).HasColumnType(StandardDBTypes.TextMax(provider));
            entity.Property(x => x.PurposeSummary).HasColumnOrder(7).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.StatusKey).HasColumnOrder(8).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.ScopeTypeKey).HasColumnOrder(9).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.ArchetypeKey).HasColumnOrder(10).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.ProductionCardinalityKey).HasColumnOrder(11).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.SpecificationSha256).HasColumnOrder(12).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.CreationDate).HasColumnOrder(13).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdatedDate).HasColumnOrder(14).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}
