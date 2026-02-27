using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational.DataContexts
{
    public class SerialNumberDataContext : DbContext
    {
        public SerialNumberDataContext(DbContextOptions<SerialNumberDataContext> optionsContext) :
            base(optionsContext)
        {

        }

        public DbSet<SerialNumberDTO> SerialNumbers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SerialNumberDTO>()
                .HasKey(a => new { a.OrgId, a.Key, a.KeyId });

            modelBuilder.LowerCaseNames(Database.ProviderName);
        }
    }
}
