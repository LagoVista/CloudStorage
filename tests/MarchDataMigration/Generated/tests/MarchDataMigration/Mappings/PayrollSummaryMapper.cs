using MarchDataMigration.Generated.PayrollRun;
using MarchDataMigration.Generated.PayrollSummary;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class PayrollSummaryMapper
    {
        public static TargetPayrollRunRow Map(SourcePayrollSummaryRow source)
        {
            return new TargetPayrollRunRow
            {
                Id = source.Id,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastupdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                OrganizationId = source.OrganizationId,
                EncryptedTotalSalary = source.EncryptedTotalSalary,
                EncryptedTotalGross = source.EncryptedTotalPayroll,
                EncryptedTotalNet = source.EncryptedTotalPayroll,
                EncryptedTotalPayroll = source.EncryptedTotalPayroll,
                EncryptedTotalExpenses = source.EncryptedTotalExpenses,
                EncryptedTotalPayrollTaxObligation = source.EncryptedTotalTaxLiability,
                EncryptedTotalRevenue = source.EncryptedTotalRevenue,
                EncryptedTaxLiabilities = source.EncryptedTaxLiabilities,
                Status = source.Status,
                Locked = source.Locked,
                LockedTimestamp = source.LockedTimeStamp,
                LockedByUserId = source.LockedByUserId,
                Approved = default,
                ApprovedTimestamp = default,
                ApprovedByUserId = default,
            };
        }
    }
}
