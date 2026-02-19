using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.IoT.Billing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class ExpenseDTO : DbModelBase
    {
        public Guid? AgreementId { get; set; }
        [Required]
        public Guid TimePeriodId { get; set; }
        public Guid? BillingEventId { get; set; }

        public Guid? VendorId { get; set; }

        public Guid? PaymentId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string UserId { get; set; }
        public Guid ExpenseCategoryId { get; set; }
        public string Notes { get; set; }
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string WorkTaskId { get; set; }
        public string WorkTaskName { get; set; }



        [Required]
        public string OrganizationId { get; set; }

        public bool Approved { get; set; }
        public string ApprovedById { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public VendorDTO Vendor { get; set; }

        public AgreementDTO Agreement { get; set; }
        public ExpenseCategoryDTO Category { get; set; }
        public PaymentDTO Payment { get; set; }
        public TimePeriodDTO TimePeriod { get; set; }

        public AppUserDTO User { get; set; }
        public AppUserDTO ApprovedUser { get; set; }

        public bool Locked { get; set; }

        public string Description { get; set; }
        public string EncryptedAmount { get; set; }
        public string EncryptedReimbursedAmount { get; set; }

    }
}
