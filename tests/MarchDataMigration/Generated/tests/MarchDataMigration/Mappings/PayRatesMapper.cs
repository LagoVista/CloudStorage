using MarchDataMigration.Generated.PayRates;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class PayRatesMapper
    {
        public static TargetPayRatesRow Map(SourcePayRatesRow source)
        {
            return new TargetPayRatesRow
            {
                Id = source.Id,
                OrganizationId = source.OrganizationId,
                UserId = source.UserId,
                Start = source.Start,
                End = source.End,
                IsSalary = source.IsSalary,
                FilingType = source.FilingType,
                DeductEstimated = source.DeductEstimated,
                DeductEstimatedRate = source.DeductEstimatedRate,
                EncryptedBillableRate = source.EncryptedBillableRate,
                EncryptedInternalRate = source.EncryptedInternalRate,
                EncryptedSalary = source.EncryptedSalary,
                EncryptedDeductions = source.EncryptedDeductions,
                EncryptedEquityScaler = source.EncryptedEquityScaler,
                Notes = source.Notes,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                WorkRoleId = source.WorkRoleId,
                IsContractor = source.IsContractor,
                IsFTE = source.IsFTE,
                IsOfficer = source.IsOfficier,
            };
        }
    }
}
