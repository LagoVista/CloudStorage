using LagoVista.Models;
using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{

    public static class AppUserDTOConfig
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUserDTO>().Property(x => x.AppUserId).HasColumnOrder(1);
            modelBuilder.Entity<AppUserDTO>().Property(x => x.Email).HasColumnOrder(2);
            modelBuilder.Entity<AppUserDTO>().Property(x => x.FullName).HasColumnOrder(3);
            modelBuilder.Entity<AppUserDTO>().Property(x => x.CreationDate).HasColumnOrder(4);
            modelBuilder.Entity<AppUserDTO>().Property(x => x.LastUpdatedDate).HasColumnOrder(5);
        }
    }

    public static class OrganizationDTOConfig
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganizationDTO>()
            .HasOne(o => o.BillingContact)
            .WithMany()
            .HasForeignKey(o => o.OrgBillingContactId);


            modelBuilder.Entity<OrganizationDTO>().Property(x => x.OrgId).HasColumnOrder(1);
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.OrgName).HasColumnOrder(2);
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.OrgBillingContactId).HasColumnOrder(3);
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.Status).HasColumnOrder(4);
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.CreationDate).HasColumnOrder(5);
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.LastUpdatedDate).HasColumnOrder(6);
        }
    }
}
