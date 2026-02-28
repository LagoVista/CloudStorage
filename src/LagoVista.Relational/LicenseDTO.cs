using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace LagoVista.Relational
{
    public class LicenseDTO : DbModelBase
    {
        public Guid AgreementLineItemId { get; set; }
        public bool IsActive { get; set; }
        public DateOnly ActiveDate { get; set; }
        public DateOnly RenewalDate { get; set; }
        public AgreementDTO Agreement { get; set; }
        public decimal QuantityUsed { get; set; }
        public decimal QuantityAllocated { get; set; }
        public List<LicenseUsageDTO> Usage { get; set; }


        public AgreementLineItemDTO AgreementLineItem { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {

        }

    }

}
