using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Concepts", Schema = "dbo")]
    public class ConceptDTO
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string OwnerOrganizationId { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string StatusKey { get; set; }

        public System.DateTime CreatedUtc { get; set; }

        public System.DateTime UpdatedUtc { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var provider = modelBuilder.GetProviderName();
            var entity = modelBuilder.Entity<ConceptDTO>();

            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OwnerOrganizationId).HasDatabaseName("IX_Concepts_OwnerOrganizationId");
            entity.HasIndex(x => x.Key).IsUnique().HasDatabaseName("UX_Concepts_Key");

            entity.Property(x => x.Id).HasColumnOrder(1).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.OwnerOrganizationId).HasColumnOrder(2).HasColumnType("varchar(32)").HasMaxLength(32);
            entity.Property(x => x.Key).HasColumnOrder(3).HasColumnType(StandardDBTypes.KeyStorage(provider));
            entity.Property(x => x.Name).HasColumnOrder(4).HasColumnType(StandardDBTypes.NameStorage(provider));
            entity.Property(x => x.Description).HasColumnOrder(5).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.StatusKey).HasColumnOrder(6).HasColumnType(StandardDBTypes.StatusStorage(provider));
            entity.Property(x => x.CreatedUtc).HasColumnOrder(7).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.UpdatedUtc).HasColumnOrder(8).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
        }
    }
}
