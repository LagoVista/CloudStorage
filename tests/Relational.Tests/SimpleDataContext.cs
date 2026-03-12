using LagoVista;
using LagoVista.Models;
using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public class SimpleDataContext : DbContext
    {

        public DbSet<SimpleRecord> Records { get; set; }


        public SimpleDataContext(DbContextOptions<SimpleDataContext> optionsContext) :
            base(optionsContext)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SeedProviderName(Database.ProviderName);


            modelBuilder.LowerCaseNames(Database.ProviderName);
            modelBuilder.ApplyUtcDateTimeConvention();
        }
    }

    [Table("Records")]
    public class SimpleRecord
    {
        [Key]
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public DateTime Timestamp { get; set; }
        public int IntValue { get; set; }
        public decimal DecimalValue { get; set; }
        public string Name { get; set; }
    }
}
