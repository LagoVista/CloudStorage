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

        public LagoVista.Core.Validation.ValidationResult Validate()
        {
            if (Id == default) Id = Guid.NewGuid();

            var result = base.ValidateCommon();
            if (CustomerId == default) result.AddSystemError("Customer id is a required field.");
            if (String.IsNullOrEmpty(Name)) result.AddUserError("Name is a required field.");
            if (Start == default || Start == DateTime.MinValue) result.AddUserError("Start date is a required field.");
            if (End == default || End == DateTime.MinValue) result.AddUserError("End date is a required field.");
            if (Start != DateTime.MinValue && Start >= End) result.AddUserError("Start Date must be prior to end date.");

            if (result.Successful)
            {
                // if agreement is not valid don't bother checking dates on child items, since dates are probably not valid.
                foreach (var item in LineItems)
                {
                    if (item.RecurringCycleTypeId == 1)
                    {
                        item.Start = null;
                        item.End = null;
                    }
                    else
                    {
                        if (item.Start.HasValue && item.Start < Start) result.AddUserError($"Start date on {item.ProductName} is {item.Start} which is prior to {Start}.");
                        if (item.End.HasValue && item.End > End) result.AddUserError($"End date on {item.ProductName} is {item.End} which is after the agreement end date of {End}.");
                    }
                }
            }

            return result;
        }

        public CustomerDTO Customer { get; set; }

        public List<InvoiceDTO> Invoices { get; set; }

        public List<AgreementLineItemDTO> LineItems { get; set; } = new List<AgreementLineItemDTO>();

    }

}
