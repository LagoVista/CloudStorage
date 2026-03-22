using System;

namespace MarchDataMigration.Generated.PayrollSummary
{
    // generated: source-side 1:1 shape
    public sealed class SourcePayrollSummaryRow
    {
        public Guid Id { get; set; }
        public string CreatedById { get; set; }
        public string LastupdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
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
    }
}
