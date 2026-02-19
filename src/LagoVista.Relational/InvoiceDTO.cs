// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 94132eec9b16acfde12e4c3d649e4fe3ade24a669d3499c07a0780787eece107
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    public class InvoiceDTO
    {
        [Key]
        public Guid Id { get; set; }


        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int InvoiceNumber { get; set; }

        public bool IsMaster { get; set; }
        public bool HasChildren { get; set; }

        public Guid? SubscriptionId { get; set; }
        public Guid? AgreementId { get; set; }
        public Guid? CustomerId { get; set; }

        public Guid? MasterInvoiceId { get; set; }

        public string ContactId { get; set; }

        public String OrgId { get; set; }
        public DateTime BillingStart { get; set; }
        public DateTime BillingEnd { get; set; }

        public DateTime CreationTimeStamp { get; set; }
        public DateTime DueDate { get; set; }
        public String Total { get; set; }
        public String Discount { get; set; }
        public String Extended { get; set; }
        public String TotalPaid { get; set; }

        public string Tax { get; set; }
        public string Shipping { get; set; }
        public string Subtotal { get; set; }


        public bool IsLocked { get; set; }

        public DateTime? PaidDate { get; set; }

        public int FailedAttemptCount { get; set; }

        public string ClosedTransactionId { get; set; }

        public string Notes { get; set; }

        public string AdditionalNotes { get; set; }

        public string Status { get; set; }
        
        public DateTime StatusDate { get; set; }

        public CustomerDTO Customer { get; set; }
       
        public AgreementDTO Agreement { get; set; }

        public OrganizationDTO Organization { get; set; }

        public SubscriptionDTO Subscription { get; set; }

        public List<InvoiceLineItemsDTO> LineItems { get; set; } = new List<InvoiceLineItemsDTO>();

        public List<InvoiceLogsDTO> Log { get; set; }
    }
}
