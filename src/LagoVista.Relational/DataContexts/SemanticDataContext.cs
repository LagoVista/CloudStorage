using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public abstract class SemanticDataContextBase : DbContext, IRelationalDiagnosticContext
    {
        protected SemanticDataContextBase(DbContextOptions options) : base(options)
        {
        }

        public bool SqlDiagnosticsEnabled { get; set; }

        public DbSet<SemanticRegistryDTO> Registry { get; set; }
        public DbSet<SubjectDTO> Subjects { get; set; }
        public DbSet<ConceptDTO> Concepts { get; set; }
        public DbSet<DefinitionDTO> Definitions { get; set; }
        public DbSet<EmbeddingDTO> Embeddings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SeedProviderName(Database.ProviderName);

            SemanticRegistryDTO.Configure(modelBuilder);
            SubjectDTO.Configure(modelBuilder);
            ConceptDTO.Configure(modelBuilder);
            DefinitionDTO.Configure(modelBuilder);
            EmbeddingDTO.Configure(modelBuilder);

            modelBuilder.LowerCaseNames(Database.ProviderName);
            modelBuilder.ApplyUtcDateTimeConvention();
        }
    }

    public class SemanticDataContext : SemanticDataContextBase
    {
        public SemanticDataContext(DbContextOptions<SemanticDataContext> options) : base(options)
        {
        }
    }

    public class SemanticTestDataContext : SemanticDataContextBase
    {
        public SemanticTestDataContext(DbContextOptions<SemanticTestDataContext> options) : base(options)
        {
        }
    }
}
