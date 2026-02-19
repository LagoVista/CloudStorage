using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.IoT.Billing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class PayRateDTO : DbModelBase
    {
        public string UserId { get; set; }
        public Guid? WorkRoleId { get; set; }
        public string OrganizationId { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Notes { get; set; }

        public bool IsFTE { get; set; }
        public bool IsContractor { get; set; }
        public bool IsOfficier { get; set; }


        public bool IsSalary { get; set; }
        public bool DeductEstimated { get; set; }
        public decimal DeductEstimatedRate { get; set; }
        public string EncryptedSalary { get; set; }
        public string EncryptedDeductions { get; set; }

        public string EncryptedEquityScaler { get; set; }
        public string EncryptedBillableRate { get; set; }
        public string EncryptedInternalRate { get; set; }
        public string FilingType { get; set; }

        [NotMapped]
        public AppUserDTO User { get; set; }

        [NotMapped]
        public decimal? Salary { get; set; }

        [NotMapped]
        public int? Deductions { get; set; }

        [NotMapped]
        public EntityHeader WorkRole { get; set; }

        [NotMapped]
        public decimal? BillableRate { get; set; }

        [NotMapped]
        public decimal? InternalRate { get; set; }

        [NotMapped]
        public decimal? EquityScaler { get; set; }
    }
}
