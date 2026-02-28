using LagoVista.Models;
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
            modelBuilder.LowerCaseNames(Database.ProviderName);


            AccountDto.Configure(modelBuilder);
            AccountTransactionCategoryDto.Configure(modelBuilder);
            AccountTransactionDto.Configure(modelBuilder);
            TransactionStagingDto.Configure(modelBuilder);
        }
    }
}
