using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace LagoVista.Relational
{
    public class SubscriptionDTO : DbModelBase
    {
   

        public SubscriptionDTO()
        {
            Icon = "icon-ae-bill-1";
            Id = Guid.NewGuid();
        }

        public string CustomerId { get; set; }

        public string PaymentToken { get; set; }
        [Required]
        public string Icon { get; set; }


        public DateTime? PaymentTokenDate { get; set; }

        public DateTime? PaymentTokenExpires { get; set; }

        [Required]
        public string PaymentTokenStatus { get; set; }

        [Required]
        public String Status { get; set; }
        [Required]
        public string Key { get; set; }
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SubscriptionDTO>()
             .HasOne(ps => ps.CreatedByUser)
             .WithMany()
             .HasForeignKey(ps => ps.CreatedById);

            modelBuilder.Entity<SubscriptionDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SubscriptionDTO>()
            .HasOne(ps => ps.Organization)
            .WithMany()
            .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.CreatedById).HasColumnOrder(2);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(3);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.CreationDate).HasColumnOrder(4);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(5);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.OrganizationId).HasColumnOrder(6);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.Name).HasColumnOrder(7);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.Key).HasColumnOrder(8);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.Status).HasColumnOrder(9);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.Description).HasColumnOrder(10);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.CustomerId).HasColumnOrder(11);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.PaymentToken).HasColumnOrder(12);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.PaymentTokenDate).HasColumnOrder(13);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.PaymentTokenExpires).HasColumnOrder(14);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.PaymentTokenStatus).HasColumnOrder(15);
            modelBuilder.Entity<SubscriptionDTO>().Property(x => x.Icon).HasColumnOrder(16);
        }
    }

}
