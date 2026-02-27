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
        public DbSet<CustomerDTO> Customers { get; set; }
        public DbSet<OrganizationDTO> Organizations { get; set; }
        public DbSet<InvoiceDTO> Invoices { get; set; }
        public DbSet<InvoiceLineItemDTO> InvoiceLineItems { get; set; }
        public DbSet<InvoiceLogsDTO> InvoiceLogs { get; set; }

        public DbSet<AgreementDTO> Agreements { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceDTO>().ToTable("invoice");
            modelBuilder.Entity<InvoiceLineItemDTO>().ToTable("InvoiceLineItems");
            modelBuilder.Entity<InvoiceLogsDTO>().ToTable("InvoiceLogs");
            modelBuilder.Entity<OrganizationDTO>().ToTable("Org");
            modelBuilder.Entity<CustomerDTO>().ToTable("Customers");
            modelBuilder.Entity<AgreementDTO>().ToTable("Agreements");


            modelBuilder.Entity<AgreementDTO>()
                .HasOne(ps => ps.CreatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<AgreementDTO>()
                .HasOne(ps => ps.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.LastUpdatedById);

            modelBuilder.Entity<AgreementDTO>()
                .HasOne(ps => ps.Organization)
                .WithMany()
                .HasForeignKey(ps => ps.OrganizationId);

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
              .WithMany()
              .HasForeignKey(li => li.OrgId);   

            modelBuilder.LowerCaseNames(Database.ProviderName);
        }
    }
}
