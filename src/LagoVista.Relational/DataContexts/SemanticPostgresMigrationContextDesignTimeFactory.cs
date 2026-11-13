using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace LagoVista.Relational.DataContexts
{
    public sealed class SemanticPostgresMigrationContextDesignTimeFactory : IDesignTimeDbContextFactory<SemanticPostgresMigrationContext>
    {
        public SemanticPostgresMigrationContext CreateDbContext(string[] args)
        {
            // The design-time connection need not be reachable when scaffolding migrations.
            // Runtime migration execution receives the connection from a Kubernetes Secret.
            var connection = Environment.GetEnvironmentVariable("MIGRATION_CONNECTION_STRING")
                ?? "Host=localhost;Port=5432;Database=semantic_design_time;Username=postgres";
            return new SemanticPostgresMigrationContext(
                new DbContextOptionsBuilder<SemanticPostgresMigrationContext>().UseNpgsql(connection).Options);
        }
    }
}
