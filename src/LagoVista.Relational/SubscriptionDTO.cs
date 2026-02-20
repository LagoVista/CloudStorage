using LagoVista.Models;
using System;

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


        public string Icon { get; set; }


        public DateTime? PaymentTokenDate { get; set; }

        public DateTime? PaymentTokenExpires { get; set; }
        public string PaymentTokenStatus { get; set; }

        public String Status { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }
    }

}
