using LagoVista.Core.Attributes;
using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("PAYROLLSUMMARY_KEY")]
    public class PayrollSummaryDTO : DbModelBase
    {
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

        [IgnoreOnMapTo]
        [MapTo("LockedByUser")]
        public AppUserDTO LockedUser { get; set; }

        [IgnoreOnMapTo]
        [NotMapped]
        public TimePeriodDTO TimePeriod { get; set; }

    
    }
}
