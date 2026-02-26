using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.Collections.Generic;

namespace LagoVista.Relational
{
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(AgreementDTO.CustomerId), CreateIfMissing = false)]
    public class AgreementDTO : DbModelBase
    {
        public Guid CustomerId { get; set; }

        [IgnoreOnMapTo()]
        public string CustomerContactId { get; set; }

        [IgnoreOnMapTo()]
        public string CustomerContactName { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public bool Locked { get; set; }
        public bool Internal { get; set; }
        public int Terms { get; set; }
        public string InvoicePeriod { get; set; }
        public DateOnly? Start { get; set; }
        public DateOnly? End { get; set; }
        public DateOnly? LastInvoicedDate { get; set; }
        public DateOnly? NextInvoiceDate { get; set; }
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


        [IgnoreOnMapTo()]
        public CustomerDTO Customer { get; set; }

        [IgnoreOnMapTo()]
        public List<InvoiceDTO> Invoices { get; set; }

        [IgnoreOnMapTo()]
        public List<AgreementLineItemDTO> LineItems { get; set; } = new List<AgreementLineItemDTO>();

    }

}
