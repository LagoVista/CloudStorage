using LagoVista.Core.Attributes;
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
    [Table("BillingEvents", Schema = "dbo")]
    public class BillingEventDTO 
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SubscriptionId { get; set; }

        public Guid ProductId { get; set; }

        /// <summary>
        /// When the billing event started
        /// </summary>
        public DateTime StartTimeStamp { get; set; }

        /// <summary>
        /// User Id of the user that initiated the billing event
        /// </summary>
        [Required]
        public String StartedByAppUserId { get; set; }

        /// <summary>
        /// When the billing event ended
        /// </summary>
        public DateTime? EndTimeStamp { get; set; }

        /// <summary>
        /// User Id of the User that Terminated the Billing Event.
        /// </summary>
        public string EndedByAppuserId { get; set; }

        /// <summary>
        /// Current Status for Billing Event, -Open, Completed, Invoiced, Error
        /// </summary>
        [Required]
        public string Status { get; set; }

        /// <summary>
        /// When the EndTimeStamp is assigned we will calculate the number of hours the resource has been
        /// used, this will be used to calculate the price/cost
        /// </summary>
        public decimal? HoursBilled { get; set; }

        /// <summary>
        /// Cost Per Unit
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Cost Per Unit
        /// </summary>
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// ShareholderType of Unit (used for calculations)
        /// </summary>
        public int UnitTypeId { get; set; }

        /// <summary>
        /// Applied Discounts
        /// </summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// Extended price for this billing period
        /// </summary>
        public decimal? Extended { get; set; }

        /// <summary>
        /// Actual resource that was used
        /// </summary>
        [Required]
        public string ResourceId { get; set; }

        /// <summary>
        /// Name of the resource that was used
        /// </summary>
        [Required]
        public string ResourceName { get; set; }

        /// <summary>
        /// Optional user entered notes
        /// </summary>
        public string Notes { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO StartedByAppUser { get; set; }

        [IgnoreOnMapTo]
        public AppUserDTO EndedByAppUser { get; set; }

        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }

        [IgnoreOnMapTo]
        public SubscriptionDTO Subscription { get; set; }   

        public static void Configure(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<BillingEventDTO>()
                .HasOne(ps => ps.Subscription)
                .WithMany(s => s.BillingEvents)
                .HasForeignKey(ps => ps.SubscriptionId);

            modelBuilder.Entity<BillingEventDTO>()
                .HasOne(ps => ps.Product)
                .WithMany()
                .HasForeignKey(ps => ps.ProductId);

            modelBuilder.Entity<BillingEventDTO>()
                .HasOne(ps => ps.StartedByAppUser)
                .WithMany()
                .HasForeignKey(ps => ps.StartedByAppUserId);

            modelBuilder.Entity<BillingEventDTO>()
                .HasOne(ps => ps.EndedByAppUser)
                .WithMany()
                .HasForeignKey(ps => ps.EndedByAppuserId);


            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.ResourceId).HasColumnOrder(2);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.ResourceName).HasColumnOrder(3);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.SubscriptionId).HasColumnOrder(4);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.ProductId).HasColumnOrder(5);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.StartTimeStamp).HasColumnOrder(6);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.StartedByAppUserId).HasColumnOrder(7);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.EndTimeStamp).HasColumnOrder(8);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.EndedByAppuserId).HasColumnOrder(9);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.HoursBilled).HasColumnOrder(10);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.UnitCost).HasColumnOrder(11);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.DiscountPercent).HasColumnOrder(12);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Extended).HasColumnOrder(13);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.UnitTypeId).HasColumnOrder(14);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Notes).HasColumnOrder(15);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Status).HasColumnOrder(16);
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.UnitPrice).HasColumnOrder(17);

            modelBuilder.Entity<BillingEventDTO>().Property(x => x.ResourceName).HasDefaultValueSql("'unknown'");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.UnitTypeId).HasDefaultValueSql("1");

            modelBuilder.Entity<BillingEventDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<BillingEventDTO>().Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.EndedByAppuserId).HasColumnType("varchar(32)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.EndTimeStamp).HasColumnType("datetime2(7)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Extended).HasColumnType("decimal(10,4)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.HoursBilled).HasColumnType("decimal(18,4)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Id).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Notes).HasColumnType("nvarchar(max)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.ProductId).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.ResourceId).HasColumnType("varchar(32)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.ResourceName).HasColumnType("varchar(255)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.StartedByAppUserId).HasColumnType("varchar(32)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.StartTimeStamp).HasColumnType("datetime2(7)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.Status).HasColumnType("varchar(50)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.SubscriptionId).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.UnitCost).HasColumnType("decimal(10,4)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            modelBuilder.Entity<BillingEventDTO>().Property(x => x.UnitTypeId).HasColumnType("int");
        }
    }
}
