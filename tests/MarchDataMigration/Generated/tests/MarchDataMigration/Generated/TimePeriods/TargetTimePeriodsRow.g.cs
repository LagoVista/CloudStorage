using System;

namespace MarchDataMigration.Generated.TimePeriods
{
    // generated: target-side 1:1 shape
    public partial class TargetTimePeriodsRow
    {
        public Guid Id { get; set; }
        public int Year { get; set; }
        public string OrganizationId { get; set; }
        public bool Locked { get; set; }
        public string LockedByUserId { get; set; }
        public DateTime? LockedTimestamp { get; set; }
        public Guid? PayrollRunId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
