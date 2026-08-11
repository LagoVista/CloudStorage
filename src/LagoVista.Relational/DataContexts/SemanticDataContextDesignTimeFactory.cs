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

            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=LagoVistaSemanticDesignTime;Trusted_Connection=True;TrustServerCertificate=True;");

            return new SemanticDataContext(optionsBuilder.Options);
        }
    }
}
