using System;

namespace MarchDataMigration.Generated.PayrollRun
{
    // generated: target-side 1:1 shape
    public partial class TargetPayrollRunRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string OrganizationId { get; set; }
        public string EncryptedTotalSalary { get; set; }
        public string EncryptedTotalGross { get; set; }
        public string EncryptedTotalNet { get; set; }
        public string EncryptedTotalPayroll { get; set; }
        public string EncryptedTotalExpenses { get; set; }
        public string EncryptedTotalPayrollTaxObligation { get; set; }
        public string EncryptedTotalRevenue { get; set; }
        public string EncryptedTaxLiabilities { get; set; }
        public string Status { get; set; }
        public bool Locked { get; set; }
        public DateTime? LockedTimestamp { get; set; }
        public string LockedByUserId { get; set; }
        public bool Approved { get; set; }
        public DateTime? ApprovedTimestamp { get; set; }
        public string ApprovedByUserId { get; set; }
    }
}
