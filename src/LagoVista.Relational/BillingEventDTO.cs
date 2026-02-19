using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
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
        public decimal UnitCost { get; set; }

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
        public string ResourceId { get; set; }

        /// <summary>
        /// Name of the resource that was used
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Optional user entered notes
        /// </summary>
        public string Notes { get; set; }


    }
}
