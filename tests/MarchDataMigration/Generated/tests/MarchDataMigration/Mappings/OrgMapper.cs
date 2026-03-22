using MarchDataMigration.Generated.Org;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class OrgMapper
    {
        public static TargetOrgRow Map(SourceOrgRow source)
        {
            return new TargetOrgRow
            {
                OrgId = source.OrgId,
                OrgName = source.OrgName,
                OrgBillingContactId = source.OrgBillingContactId,
                Status = source.Status,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdatedDate,
            };
        }
    }
}
