using MarchDataMigration.Generated.TimePeriods;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class TimePeriodsMapper
    {
        public static TargetTimePeriodsRow Map(SourceTimePeriodsRow source)
        {
            return new TargetTimePeriodsRow
            {
                Id = source.Id,
                Year = source.Year,
                OrganizationId = source.OrganizationId,
                Locked = source.Locked,
                LockedByUserId = source.LockedByUserId,
                LockedTimestamp = source.LockedTimeStamp,
                PayrollRunId = default,
                Start = source.Start,
                End = source.End,
            };
        }
    }
}
