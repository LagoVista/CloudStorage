using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.Collections.Generic;

namespace LagoVista.Relational
{
    public class AgreementDTO : DbModelBase
    {
        public Guid CustomerId { get; set; }

        public string CustomerContactId { get; set; }
        public string CustomerContactName { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public bool Locked { get; set; }
        public bool Internal { get; set; }
        public int Terms { get; set; }
        public string InvoicePeriod { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public DateTime? LastInvoicedDate { get; set; }
        public DateTime? NextInvoiceDate { get; set; }
        public string Status { get; set; }
        public decimal? Hours { get; set; }
        public string EncryptedRate { get; set; }
        public string Notes { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal Tax { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }


        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), Name);
        }

       

        public CustomerDTO Customer { get; set; }

        public List<InvoiceDTO> Invoices { get; set; }

        public List<AgreementLineItemDTO> LineItems { get; set; } = new List<AgreementLineItemDTO>();

    }

}
