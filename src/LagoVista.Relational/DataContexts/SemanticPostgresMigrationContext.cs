using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    // Separate migrations identity prevents EF from replaying the legacy SQL Server
    // migration history against PostgreSQL. Both logical registries share this schema.
    public sealed class SemanticPostgresMigrationContext : SemanticDataContextBase
    {
        public SemanticPostgresMigrationContext(DbContextOptions<SemanticPostgresMigrationContext> options) : base(options) { }
    }
}
