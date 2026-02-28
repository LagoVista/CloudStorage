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

            modelBuilder.Entity<AppUserDTO>().HasKey(x => new { x.AppUserId });

            modelBuilder.Entity<AppUserDTO>().Property(x => x.AppUserId).HasColumnType("varchar(32)");
            modelBuilder.Entity<AppUserDTO>().Property(x => x.CreationDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<AppUserDTO>().Property(x => x.Email).HasColumnType("varchar(max)");
            modelBuilder.Entity<AppUserDTO>().Property(x => x.FullName).HasColumnType("varchar(max)");
            modelBuilder.Entity<AppUserDTO>().Property(x => x.LastUpdatedDate).HasColumnType("datetime2(7)");
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

            modelBuilder.Entity<OrganizationDTO>().HasKey(x => new { x.OrgId });

            modelBuilder.Entity<OrganizationDTO>().Property(x => x.CreationDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.LastUpdatedDate).HasColumnType("datetime2(7)");
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.OrgBillingContactId).HasColumnType("varchar(32)");
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.OrgId).HasColumnType("varchar(32)");
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.OrgName).HasColumnType("varchar(max)");
            modelBuilder.Entity<OrganizationDTO>().Property(x => x.Status).HasColumnType("varchar(50)");
        }
    }
}
