using LagoVista.Core;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    [Table("WorkRoles",Schema="dbo")]
    public class WorkRoleDTO : DbModelBase
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Key { get; set; }
        [Required] 
        public string Description { get; set; }

        [Required]
        public string Icon { get; set; }
        public bool IsActive { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkRoleDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<WorkRoleDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WorkRoleDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.Key).HasColumnOrder(3);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.Name).HasColumnOrder(4);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.Icon).HasColumnOrder(5);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.IsActive).HasColumnOrder(6);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.Description).HasColumnOrder(7);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.CreationDate).HasColumnOrder(8);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.CreatedById).HasColumnOrder(9);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(10);
            modelBuilder.Entity<WorkRoleDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(11);

            modelBuilder.Entity<WorkRoleDTO>().HasKey(x => new { x.Id });
        }
    }
}
