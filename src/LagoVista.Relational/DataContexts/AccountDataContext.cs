// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 19ae50d0c54d38710027ff3afdf7615d2da0e2100d3a1a7a3fa62b1ffb2ac2a7
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.IoT.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public class AccountDataContext : DbContext
    {
        public AccountDataContext(DbContextOptions<AccountDataContext> optionsContext) :
            base(optionsContext)
        {

        }

        public DbSet<AppUserDTO> AppUser { get; set; }
        public DbSet<OrganizationDTO> Organizations { get; set; }
        public DbSet<AccountDto> Account { get; set; }
        public DbSet<AccountTransactionCategoryDto> TransactionCategory { get; set; }
        public DbSet<AccountTransactionDto> Transaction { get; set; }
        public DbSet<TransactionStagingDto> TransactionStaging { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<OrganizationDTO>().ToTable("Org");
            modelBuilder.Entity<AccountTransactionCategoryDto>().ToTable("AccountTransactionCategory");
            modelBuilder.Entity<AccountTransactionDto>().ToTable("AccountTransaction");
            modelBuilder.Entity<AccountDto>().ToTable("Account");
            modelBuilder.Entity<WorkRoleDTO>().ToTable("WorkRoles");
            modelBuilder.Entity<BudgetItemDTO>().ToTable("BudgetItems");
            modelBuilder.Entity<VendorDTO>().ToTable("Vendor");
            modelBuilder.Entity<TransactionStagingDto>().ToTable("TransactionStaging");

            modelBuilder.Entity<AccountDto>()
                .HasOne(ps => ps.CreatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<AccountDto>()
                .HasOne(ps => ps.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.LastUpdatedById);

            modelBuilder.Entity<AccountDto>()
                .HasOne(ps => ps.Organization)
                .WithMany()
                .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.Vendor)
                .WithMany()
                .HasForeignKey(ps => ps.VendorId);

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.Category)
                .WithMany()
                .HasForeignKey(ps => ps.TransactionCategoryId);

            modelBuilder.Entity<TransactionStagingDto>()
                .HasOne(ps => ps.Account)
                .WithMany()
                .HasForeignKey(ps => ps.AccountId);

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.Account)
                .WithMany()
                .HasForeignKey(ps => ps.AccountId);

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.CreatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<AccountTransactionDto>()
                .HasOne(ps => ps.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.LastUpdatedById);


            modelBuilder.Entity<AccountTransactionCategoryDto>()
                .HasOne(ps => ps.CreatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<AccountTransactionCategoryDto>()
                .HasOne(ps => ps.ExpenseCategory)
                .WithMany()
                .HasForeignKey(ps => ps.ExpenseCategoryId);


            modelBuilder.Entity<AccountTransactionCategoryDto>()
                .HasOne(ps => ps.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ps => ps.LastUpdatedById);

            modelBuilder.Entity<AccountTransactionCategoryDto>()
                .HasOne(ps => ps.Organization)
                .WithMany()
                .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.LowerCaseNames();
        }
    }
}
