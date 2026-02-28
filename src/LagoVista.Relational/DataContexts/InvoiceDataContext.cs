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

            modelBuilder.Entity<CustomerDTO>()
                 .HasOne(ps => ps.CreatedByUser)
                 .WithMany()
                 .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<CustomerDTO>()
                .HasOne(ps => ps.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.LastUpdatedById);

            modelBuilder.Entity<InvoiceDTO>()
                .HasOne(inv => inv.Agreement)
                .WithMany(agr => agr.Invoices)
                .HasForeignKey(inv => inv.AgreementId);

            modelBuilder.Entity<InvoiceLineItemDTO>()
                .HasOne(li => li.Invoice)
                .WithMany(inv => inv.LineItems)
                .HasForeignKey(li => li.InvoiceId);


            modelBuilder.Entity<InvoiceDTO>()
                .HasOne(inv => inv.Customer)
                .WithMany(cst => cst.Invoices)
                .HasForeignKey(li => li.CustomerId);

            modelBuilder.Entity<InvoiceDTO>()
              .HasOne(inv => inv.Organization)
              .WithMany()
              .HasForeignKey(li => li.OrgId);



            modelBuilder.LowerCaseNames(Database.ProviderName);

            AgreementDTO.Configure(modelBuilder);
            AgreementLineItemDTO.Configure(modelBuilder);
            InvoiceDTO.Configure(modelBuilder);
            InvoiceLineItemDTO.Configure(modelBuilder);
            InvoiceLogsDTO.Configure(modelBuilder);

            CustomerDTO.Configure(modelBuilder);

        }
    }
}
