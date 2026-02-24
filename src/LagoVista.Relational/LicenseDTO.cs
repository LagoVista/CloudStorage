using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class LicenseDTO
    {
        public Guid Id { get; set; }
        public Guid AgreementLineItemId { get; set; }
        public AgreementLineItemDTO AgreementLineItem { get; set; }
        public bool IsActive { get; set; }
        public DateOnly ActiveDate { get; set; }
        public DateOnly RenewalDate { get; set; }
        public AgreementDTO Agreement { get; set; }
        public decimal QuantityUsed { get; set; }
        public decimal QuantityAllocated { get; set; }
        public List<LicenseUsageDTO> Usage { get; set; }
    }

}
