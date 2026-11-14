using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational.DataContexts
{
    public sealed class SemanticDataContextDesignTimeFactory : IDesignTimeDbContextFactory<SemanticDataContext>
    {
        public SemanticDataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SemanticDataContext>();

            // InfoElements design-time model is PostgreSQL too. Use the dedicated
            // SemanticPostgresMigrationContext to scaffold migrations.
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=semantic_design_time;Username=postgres");

            return new SemanticDataContext(optionsBuilder.Options);
        }
    }
}
