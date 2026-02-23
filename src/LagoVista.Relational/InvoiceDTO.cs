using LagoVista.Core.Attributes;
using LagoVista.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("Agreement-{id}", IdProperty = nameof(CustomerId), CreateIfMissing = false)]
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
        public DateOnly BillingStart { get; set; }
        public DateOnly BillingEnd { get; set; }

        public DateTime CreationTimeStamp { get; set; }
        public DateOnly DueDate { get; set; }
        public String Total { get; set; }
        public String Discount { get; set; }
        public String Extended { get; set; }
        public String TotalPaid { get; set; }

        public string Tax { get; set; }
        public string Shipping { get; set; }
        public string Subtotal { get; set; }


        public bool IsLocked { get; set; }

        public DateOnly? PaidDate { get; set; }

        public DateOnly InvoiceDate { get; set; }

        public int FailedAttemptCount { get; set; }

        public string ClosedTransactionId { get; set; }

        public string Notes { get; set; }

        public string AdditionalNotes { get; set; }

        public string Status { get; set; }
        
        public DateTime StatusDate { get; set; }

        [IgnoreOnMapTo]
        public CustomerDTO Customer { get; set; }

        [IgnoreOnMapTo]
        public AgreementDTO Agreement { get; set; }

        [MapTo("OwnerOrganization")]
        [IgnoreOnMapTo]
        public OrganizationDTO Organization { get; set; }

        [IgnoreOnMapTo]
        public SubscriptionDTO Subscription { get; set; }

        [IgnoreOnMapTo]
        public List<InvoiceLineItemDTO> LineItems { get; set; } = new List<InvoiceLineItemDTO>();

        [IgnoreOnMapTo]
        public List<InvoiceLogsDTO> Log { get; set; }
    }
}
