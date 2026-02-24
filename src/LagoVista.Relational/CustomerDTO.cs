using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace LagoVista.Relational
{
    public class CustomerDTO : DbModelBase
    {
        public string CustomerName { get; set; }
        public string BillingContactName { get; set; }
        public string BillingContactEmail { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Notes { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }

        [IgnoreOnMapTo]
        public IEnumerable<InvoiceDTO> Invoices { get; set; }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), CustomerName);
        }
    }
}
