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


        public DbSet<CustomerDTO> Customers { get; set; }

        public DbSet<OrganizationDTO> Organizations { get; set; }

        public DbSet<InvoiceDTO> Invoices { get; set; }
        public DbSet<InvoiceLineItemsDTO> InvoiceLineItems { get; set; }
        public DbSet<InvoiceLogsDTO> InvoiceLogs { get; set; }

        public DbSet<AgreementDTO> Agreements { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceDTO>().ToTable("invoice");
            modelBuilder.Entity<InvoiceLineItemsDTO>().ToTable("InvoiceLineItems");
            modelBuilder.Entity<InvoiceLogsDTO>().ToTable("InvoiceLogs");
            modelBuilder.Entity<OrganizationDTO>().ToTable("Org");
            modelBuilder.Entity<CustomerDTO>().ToTable("Customers");
            modelBuilder.Entity<AgreementDTO>().ToTable("Agreements");

            modelBuilder.Entity<InvoiceDTO>()
                .HasOne(inv => inv.Agreement)
                .WithMany(agr => agr.Invoices)
                .HasForeignKey(inv => inv.AgreementId);

            modelBuilder.Entity<InvoiceLineItemsDTO>()
                .HasOne(li => li.Invoice)
                .WithMany(inv => inv.LineItems)
                .HasForeignKey(li => li.InvoiceId);

            modelBuilder.Entity<InvoiceLogsDTO>()
                .HasOne(li => li.Invoice)
                .WithMany(inv => inv.Log)
                .HasForeignKey(li => li.InvoiceId);

            modelBuilder.Entity<InvoiceDTO>()
                .HasOne(inv => inv.Customer)
                .WithMany(cst => cst.Invoices)
                .HasForeignKey(li => li.CustomerId);

            modelBuilder.Entity<InvoiceDTO>()
              .HasOne(inv => inv.Organization)
              .WithMany(cst => cst.Invoices)
              .HasForeignKey(li => li.OrgId);

            modelBuilder.LowerCaseNames();
        }
    }
}
