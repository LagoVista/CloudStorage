using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational.DataContexts
{
    public class LicenseDataContext : DbContext
    {
        public DbSet<AgreementDTO> Agreements { get; set; }

        public DbSet<AgreementLineItemDTO> AgreementLineItems { get; set; }

        public DbSet<LicenseDTO> License { get; set; }

        public DbSet<LicenseUsageDTO> LicenseUsage { get; set; }

        public LicenseDataContext(DbContextOptions<InvoiceDataContext> optionsContext) :
            base(optionsContext)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            modelBuilder.LowerCaseNames(Database.ProviderName);
        }

    }
}
