using MarchDataMigration.Generated.TimeEntries;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class TimeEntriesMapper
    {
        public static TargetTimeEntriesRow Map(SourceTimeEntriesRow source)
        {
            return new TargetTimeEntriesRow
            {
                Id = source.Id,
                AgreementId = source.AgreementId,
                TimePeriodId = source.TimePeriodId,
                BillingEventId = source.BillingEventId,
                Date = source.Date,
                OrganizationId = source.OrganizationId,
                ProjectId = source.ProjectId,
                ProjectName = source.ProjectName,
                WorkTaskId = source.WorkTaskId,
                WorkTaskName = source.WorkTaskName,
                UserId = source.UserId,
                Locked = source.Locked,
                IsEquityTime = source.IsEquityTime,
                Hours = source.Hours,
                Notes = source.Notes,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
            };
        }
    }
}
