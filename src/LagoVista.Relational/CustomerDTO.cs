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

        public LagoVista.Core.Validation.ValidationResult Validate()
        {
            if (Id == default) Id = Guid.NewGuid();

            var result = base.ValidateCommon();
            if (String.IsNullOrEmpty(CustomerName)) result.AddUserError("Customer Name is a required field.");
            if (String.IsNullOrEmpty(BillingContactName)) result.AddUserError("Billing Contact Name is a required field.");
            if (String.IsNullOrEmpty(BillingContactEmail))
                result.AddUserError("Billing Contact Email is a required field.");
            else if (!Regex.IsMatch(BillingContactEmail, @"^\w+@[a-zA-Z_]+?\.[a-zA-Z]{2,3}$"))
                result.AddUserError($"Invalid email address for billing contact: {BillingContactEmail}.");

            return result;
        }


        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), CustomerName);
        }
    }
}
