// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 036125d5de09cabcdfaecbf8fb28f0a52fb2b8f1b05c98d5d82c1947e7ac079a
// IndexVersion: 2
// --- END CODE INDEX META ---
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LagoVista.Relational
{
    public class InvoiceLineItemsDTO
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InvoiceId { get; set; }

        public string ResourceId { get; set; }

        public string ResourceName { get; set; }

        public string ProductName { get; set; }
        public Guid? ProductId { get; set; }

        public decimal Quantity { get; set; }
        public string Units { get; set; }

        public bool? Taxable { get; set; }
        public string UnitPrice { get; set; }
        public string Total { get; set; }
        public string Discount { get; set; }
        public string Extended { get; set; }
        public string Shipping { get; set; }

        public InvoiceDTO Invoice { get; set; }

    }
}
