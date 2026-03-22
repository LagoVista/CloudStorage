using System;

namespace MarchDataMigration.Generated.TimeEntries
{
    // generated: source-side 1:1 shape
    public sealed class SourceTimeEntriesRow
    {
        public Guid Id { get; set; }
        public Guid AgreementId { get; set; }
        public Guid TimePeriodId { get; set; }
        public Guid? BillingEventId { get; set; }
        public DateTime Date { get; set; }
        public string OrganizationId { get; set; }
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string WorkTaskId { get; set; }
        public string WorkTaskName { get; set; }
        public string UserId { get; set; }
        public bool Locked { get; set; }
        public bool IsEquityTime { get; set; }
        public decimal Hours { get; set; }
        public string Notes { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
    }
}
