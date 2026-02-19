using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class AgreementLineItemDTO
    {
        public Guid Id { get; set; }
        public Guid AgreementId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }

        public DateTime? Start { get; set; }

        public DateTime? End { get; set; }

        public DateTime? NextBillingDate { get; set; }
        public DateTime? LastBilledDate { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Extended { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Shipping { get; set; }

        public decimal Quantity { get; set; }

        public bool Taxable { get; set; }

        public int UnitTypeId { get; set; }

        public bool IsRecurring { get; set; }
        public int RecurringCycleTypeId { get; set; }

        public ProductDTO Product { get; set; }

        public AgreementDTO Agreement { get; set; }
    }
}
