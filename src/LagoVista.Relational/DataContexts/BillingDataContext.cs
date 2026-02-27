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

        public DbSet<BillingEventDTO> BillingEvents { get; set; }
        public DbSet<TimeEntryDTO> TimeEntries { get; set; }
        public DbSet<PayrollSummaryDTO> PayrollSummary { get; set; }
        public DbSet<TimePeriodDTO> TimePeriods { get; set; }

        public DbSet<CustomerDTO> Customers { get; set; }
        public DbSet<AgreementDTO> Agreements { get; set; }

        public DbSet<AgreementLineItemDTO> AgreementLineItems { get; set; }

        public DbSet<ExpenseDTO> Expenses { get; set; }

        public DbSet<AccountTransactionCategoryDto> AccountTransactionCategory { get; set; }
        public DbSet<PayRateDTO> PayRates { get; set; }
        public DbSet<AppUserDTO> AppUser { get; set; }
        public DbSet<OrganizationDTO> Org { get; set; }
        public DbSet<PaymentDTO> Payments { get; set; }
        public DbSet<WorkRoleDTO> WorkRoles { get; set; }
        public DbSet<BudgetItemDTO> BudgetItems { get; set; }
        public DbSet<ExpenseCategoryDTO> ExpenseCategory { get; set; }

        public DbSet<VendorDTO> Vendor { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountTransactionCategoryDto>().ToTable("AccountTransactionCategory");
            modelBuilder.Entity<WorkRoleDTO>().ToTable("WorkRoles");
            modelBuilder.Entity<VendorDTO>().ToTable("Vendor");

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(at => at.Vendor)
                .WithMany()
                .HasForeignKey(at => at.VendorId);

            modelBuilder.Entity<TimePeriodDTO>()
            .HasOne(tp => tp.PayrollSummary)
            .WithOne(ps => ps.TimePeriod)
            .HasForeignKey<TimePeriodDTO>(tp => tp.PayrollSummaryId);

            modelBuilder.Entity<PayRateDTO>()
            .HasOne(tp => tp.User)
            .WithMany()
            .HasForeignKey(tp => tp.UserId);

            modelBuilder.Entity<PaymentDTO>()
            .HasOne(py => py.User)
            .WithMany()
            .HasForeignKey(tp => tp.UserId);

            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ps => ps.LockedUser)
            .WithMany()
            .HasForeignKey(ps => ps.LockedByUserId);

            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WorkRoleDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<WorkRoleDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WorkRoleDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AgreementDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AgreementDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AgreementDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AgreementLineItemDTO>()
            .HasOne(li => li.Product)
            .WithMany()
            .HasForeignKey(li => li.ProductId);

            modelBuilder.Entity<PayrollSummaryDTO>()
            .HasOne(ex => ex.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Agreement)
            .WithMany()
            .HasForeignKey(x => x.AgreementId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Category)
            .WithMany()
            .HasForeignKey(x => x.ExpenseCategoryId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Vendor)
            .WithMany()
            .HasForeignKey(ex => ex.VendorId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.TimePeriod)
            .WithMany()
            .HasForeignKey(x => x.TimePeriodId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.ApprovedUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedById);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(x => x.LastUpdatedById);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.AccountTransactionCategory)
            .WithMany()
            .HasForeignKey(ps => ps.AccountTransactionCategoryId);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.ExpenseCategory)
            .WithMany()
            .HasForeignKey(ps => ps.ExpenseCategoryId);

            modelBuilder.Entity<BudgetItemDTO>()
            .HasOne(ps => ps.WorkRole)
            .WithMany()
            .HasForeignKey(ps => ps.WorkRoleId);


            modelBuilder.Entity<ExpenseCategoryDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExpenseCategoryDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExpenseCategoryDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.DefaultExpenseCategory)
            .WithMany()
            .HasForeignKey(ps => ps.DefaultExpenseCategoryId);

            modelBuilder.Entity<VendorDTO>()
            .HasOne(ps => ps.DefaultAccountTransactionCategory)
            .WithMany()
            .HasForeignKey(ps => ps.DefaultAccountTransactionCategoryId);

            modelBuilder.LowerCaseNames(Database.ProviderName);
        }
    }


}
