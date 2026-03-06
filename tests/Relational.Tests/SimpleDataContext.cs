using LagoVista;
using LagoVista.Models;
using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public class SimpleDataContext : DbContext
    {
        public DbSet<AccountDto> Account { get; set; }


        public SimpleDataContext(DbContextOptions<SimpleDataContext> optionsContext) :
            base(optionsContext)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SeedProviderName(Database.ProviderName);

            AccountDto.Configure(modelBuilder);

            modelBuilder.LowerCaseNames(Database.ProviderName);
            modelBuilder.ApplyUtcDateTimeConvention();
        }
    }
}
