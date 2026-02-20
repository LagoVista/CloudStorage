using LagoVista.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class OrganizationWithInvoicesDTO : OrganizationDTO
    {
        public List<InvoiceDTO> Invoices { get; set; }
    }
}
