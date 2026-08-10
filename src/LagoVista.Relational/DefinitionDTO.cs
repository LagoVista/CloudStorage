using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Definitions", Schema = "dbo")]
    public class DefinitionDTO
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string SubjectId { get; set; }

        [Required]
        public string ConceptId { get; set; }

        [Required]
        public string QualifiedKey { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Summary { get; set; }

        public string Example1 { get; set; }

        public string Example2 { get; set; }

        public string Example3 { get; set; }

        [Required]
        public string StatusKey { get; set; }

        [Required]
        public string DefinitionSha256 { get; set; }

        public System.DateTime CreatedUtc { get; set; }

        public System.DateTime UpdatedUtc { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var provider = modelBuilder.GetProviderName();
            var entity = modelBuilder.Entity<DefinitionDTO>();

            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.QualifiedKey).IsUnique().HasDatabaseName("UX_Definitions_QualifiedKey");
            entity.HasIndex(x => x.SubjectId).HasDatabaseName("IX_Definitions_SubjectId");
            entity.HasIndex(x => x.ConceptId).HasDatabaseName("IX_Definitions_ConceptId");

            entity.HasOne<SubjectDTO>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ConceptDTO>().WithMany().HasForeignKey(x => x.ConceptId).OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.Id).HasColumnOrder(1).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.SubjectId).HasColumnOrder(2).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.ConceptId).HasColumnOrder(3).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.QualifiedKey).HasColumnOrder(4).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Name).HasColumnOrder(5).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Summary).HasColumnOrder(6).HasColumnType(StandardDBTypes.TextLong(provider));
            entity.Property(x => x.Example1).HasColumnOrder(7).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Example2).HasColumnOrder(8).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.Example3).HasColumnOrder(9).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.StatusKey).HasColumnOrder(10).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.DefinitionSha256).HasColumnOrder(11).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.CreatedUtc).HasColumnOrder(12).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.UpdatedUtc).HasColumnOrder(13).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}
