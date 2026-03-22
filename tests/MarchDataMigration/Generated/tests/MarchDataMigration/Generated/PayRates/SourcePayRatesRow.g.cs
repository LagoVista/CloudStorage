using System;

namespace MarchDataMigration.Generated.PayRates
{
    // generated: source-side 1:1 shape
    public sealed class SourcePayRatesRow
    {
        public Guid Id { get; set; }
        public string OrganizationId { get; set; }
        public string UserId { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public bool IsSalary { get; set; }
        public string FilingType { get; set; }
        public bool DeductEstimated { get; set; }
        public decimal DeductEstimatedRate { get; set; }
        public string EncryptedBillableRate { get; set; }
        public string EncryptedInternalRate { get; set; }
        public string EncryptedSalary { get; set; }
        public string EncryptedDeductions { get; set; }
        public string EncryptedEquityScaler { get; set; }
        public string Notes { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public Guid? WorkRoleId { get; set; }
        public bool IsContractor { get; set; }
        public bool IsFTE { get; set; }
        public bool IsOfficier { get; set; }
    }
}
