using LagoVista.Models;
using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public class BillingDataContext : DbContext
    {
        public BillingDataContext(DbContextOptions<BillingDataContext> optionsContext) :
            base(optionsContext)
        {

        }


        public DbSet<AppUserDTO> AppUser { get; set; }
        public DbSet<OrganizationDTO> Org { get; set; }

        public DbSet<AccountTransactionCategoryDto> AccountTransactionCategory { get; set; }


        public DbSet<AgreementDTO> Agreements { get; set; }
        public DbSet<AgreementLineItemDTO> AgreementLineItems { get; set; }


        public DbSet<BillingEventDTO> BillingEvents { get; set; }
        public DbSet<BudgetItemDTO> BudgetItems { get; set; }


        public DbSet<CustomerDTO> Customers { get; set; }

        public DbSet<ExpenseCategoryDTO> ExpenseCategory { get; set; }
        public DbSet<ExpenseDTO> Expenses { get; set; }

        public DbSet<InvoiceDTO> Invoices { get; set; }
        public DbSet<InvoiceDTO> InvoiceLineItems { get; set; }

        public DbSet<PayrollSummaryDTO> PayrollSummary { get; set; }
        public DbSet<PayRateDTO> PayRates { get; set; }
        public DbSet<PaymentDTO> Payments { get; set; }

        public DbSet<TimeEntryDTO> TimeEntries { get; set; }
        public DbSet<TimePeriodDTO> TimePeriods { get; set; }

        public DbSet<VendorDTO> Vendor { get; set; }

        public DbSet<WorkRoleDTO> WorkRoles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            AgreementDTO.Configure(modelBuilder);
            AgreementLineItemDTO.Configure(modelBuilder);

            AccountTransactionCategoryDto.Configure(modelBuilder);

            BillingEventDTO.Configure(modelBuilder);

            BudgetItemDTO.Configure(modelBuilder);

            CustomerDTO.Configure(modelBuilder);

            ExpenseDTO.Configure(modelBuilder);
            ExpenseCategoryDTO.Configure(modelBuilder);

            InvoiceDTO.Configure(modelBuilder);
            InvoiceLineItemDTO.Configure(modelBuilder);

            PaymentDTO.Configure(modelBuilder);
            PayRateDTO.Configure(modelBuilder);
            PayrollSummaryDTO.Configure(modelBuilder);

            TimeEntryDTO.Configure(modelBuilder);
            TimePeriodDTO.Configure(modelBuilder);

            VendorDTO.Configure(modelBuilder);

            WorkRoleDTO.Configure(modelBuilder);

            modelBuilder.LowerCaseNames(Database.ProviderName);
        }
    }
}
