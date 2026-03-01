using LagoVista.Models;
using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public class InvoiceDataContext : DbContext
    {
        public InvoiceDataContext(DbContextOptions<InvoiceDataContext> optionsContext) :
            base(optionsContext)
        {
        }

        public DbSet<AppUserDTO> AppUser { get; set; }
        public DbSet<OrganizationDTO> Organizations { get; set; }


        public DbSet<CustomerDTO> Customers { get; set; }
        public DbSet<AgreementDTO> Agreements { get; set; }
        public DbSet<AgreementLineItemDTO> AgreementLineItems { get; set; }

        public DbSet<InvoiceDTO> Invoices { get; set; }
        public DbSet<InvoiceLineItemDTO> InvoiceLineItems { get; set; }
        public DbSet<InvoiceLogsDTO> InvoiceLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SeedProviderName(Database.ProviderName);

            AppUserDTOConfig.Configure(modelBuilder);
            OrganizationDTOConfig.Configure(modelBuilder);
            AgreementDTO.Configure(modelBuilder);
            AgreementLineItemDTO.Configure(modelBuilder);
            CustomerDTO.Configure(modelBuilder);
            InvoiceDTO.Configure(modelBuilder);
            InvoiceLineItemDTO.Configure(modelBuilder);
            InvoiceLogsDTO.Configure(modelBuilder);

            modelBuilder.LowerCaseNames(Database.ProviderName);
            modelBuilder.ApplyUtcDateTimeConvention();
        }
    }
}
