using System;

namespace MarchDataMigration.Generated.TimePeriods
{
    // generated: source-side 1:1 shape
    public sealed class SourceTimePeriodsRow
    {
        public Guid Id { get; set; }
        public int Year { get; set; }
        public string OrganizationId { get; set; }
        public bool Locked { get; set; }
        public string LockedByUserId { get; set; }
        public DateTime? LockedTimeStamp { get; set; }
        public Guid? PayrollSummaryId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
