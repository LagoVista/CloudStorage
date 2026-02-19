using LagoVista.Core;
using LagoVista.IoT.Billing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class PayrollSummaryDTO : DbModelBase
    {
        public string OrganizationId { get; set; }
        public string EncryptedTotalSalary { get; set; }
        public string EncryptedTotalPayroll { get; set; }
        public string EncryptedTotalExpenses { get; set; }
        public string EncryptedTotalTaxLiability { get; set; }
        public string EncryptedTotalRevenue { get; set; }
        public string EncryptedTaxLiabilities { get; set; }
        public string Status { get; set; }
        public bool Locked { get; set; }
        public DateTime? LockedTimeStamp { get; set; }
        public string LockedByUserId { get; set; }

        [NotMapped]
        public AppUserDTO LockedUser { get; set; }

        [NotMapped]
        public TimePeriodDTO TimePeriod { get; set; }

    
    }
}
