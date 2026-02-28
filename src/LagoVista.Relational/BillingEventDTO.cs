using LagoVista.Core.Attributes;
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

        public static void Configure(ModelBuilder modelBuilder)
        {
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

        }

    }
}
