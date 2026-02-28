using LagoVista.Models;
using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public class UserAdminDataContext : DbContext
    {
        public UserAdminDataContext(DbContextOptions<UserAdminDataContext> contextOptions) : base(contextOptions)
        {

        }

        public DbSet<OrganizationDTO> Org { get; set; }
        public DbSet<AppUserDTO> AppUser { get; set; }
        public DbSet<SubscriptionDTO> Subscription { get; set; }
        public DbSet<DeviceOwnerDTO> DeviceOwnerUser { get; set; }
        public DbSet<OwnedDeviceDTO> DeviceOwnerUserDevices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            AppUserDTOConfig.Configure(modelBuilder);
            OrganizationDTOConfig.Configure(modelBuilder);
            OwnedDeviceDTO.Configure(modelBuilder);
            DeviceOwnerDTO.Configure(modelBuilder);
            SubscriptionDTO.Configure(modelBuilder);

            modelBuilder.LowerCaseNames(Database.ProviderName);
        }


    }

}
