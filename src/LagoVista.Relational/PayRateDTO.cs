using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("Rate-{id}", IdProperty = nameof(PayRateDTO.UserId), CreateIfMissing = true)]
    public class PayRateDTO : DbModelBase
    {
        public string UserId { get; set; }
        public Guid? WorkRoleId { get; set; }
        public DateOnly Start { get; set; }
        public DateOnly? End { get; set; }
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
        [IgnoreOnMapTo]
        public AppUserDTO User { get; set; }

        [NotMapped]
        [IgnoreOnMapTo]
        public EntityHeader WorkRole { get; set; }
    }
}
