using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public class LicenseDataContext : DbContext
    {
        public DbSet<AgreementDTO> Agreements { get; set; }

        public DbSet<AgreementLineItemDTO> AgreementLineItems { get; set; }

        public DbSet<LicenseDTO> License { get; set; }

        public DbSet<LicenseUsageDTO> LicenseUsage { get; set; }

        public LicenseDataContext(DbContextOptions<LicenseDataContext> optionsContext) :
            base(optionsContext)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            AgreementDTO.Configure(modelBuilder);
            AgreementLineItemDTO.Configure(modelBuilder);
            LicenseDTO.Configure(modelBuilder);
            LicenseUsageDTO.Configure(modelBuilder);

            modelBuilder.SeedProviderName(Database.ProviderName);
            modelBuilder.LowerCaseNames(Database.ProviderName);
            modelBuilder.ApplyUtcDateTimeConvention();
        }
    }
}
