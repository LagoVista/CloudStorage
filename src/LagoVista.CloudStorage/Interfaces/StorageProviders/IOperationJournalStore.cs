using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>Durable journal for verified deterministic operation boundaries, not model activity.</summary>
    public interface IOperationJournalStore
    {
        Task<OperationJournalRecord> StartAsync(OperationJournalRecord operation, CancellationToken cancellationToken = default);
        Task<OperationJournalRecord> RecoverAsync(string organizationId, string operationId, string expectedStatus,
            string summary, DateTimeOffset recoveredAtUtc, CancellationToken cancellationToken = default);
        Task<OperationJournalRecord> TransitionAsync(string organizationId, string operationId, string expectedStatus, string nextStatus,
            string summary, DateTimeOffset changedAtUtc, CancellationToken cancellationToken = default);
        Task<OperationJournalDetail> AppendAsync(OperationJournalDetail detail, CancellationToken cancellationToken = default);
        Task<OperationJournalRecord> GetAsync(string organizationId, string operationId, CancellationToken cancellationToken = default);
        Task<OperationJournalPage<OperationJournalRecord>> ListAsync(OperationJournalScope scope, int pageSize, string continuationToken = null,
            CancellationToken cancellationToken = default);
        Task<OperationJournalPage<OperationJournalDetail>> GetDetailsAsync(string organizationId, string operationId, int pageSize,
            string continuationToken = null, CancellationToken cancellationToken = default);
    }

    /// <summary>A bounded tenant-authorized query. Workstream includes its child Workspace operations.</summary>
    public sealed class OperationJournalScope
    {
        public string OrganizationId { get; set; }
        public string ScopeType { get; set; }
        public string WorkstreamId { get; set; }
        public string WorkspaceId { get; set; }
        public string FixWorkspaceId { get; set; }
    }

    public sealed class OperationJournalRecord
    {
        public string OrganizationId { get; set; }
        public string OperationId { get; set; }
        public string CommandId { get; set; }
        public string BoundaryType { get; set; }
        public string OwnerType { get; set; }
        public string OwnerId { get; set; }
        public string WorkstreamId { get; set; }
        public string WorkspaceId { get; set; }
        public string FixWorkspaceId { get; set; }
        public string Status { get; set; }
        public string Summary { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
        public int RecoveryCount { get; set; }
        public string EvidenceId { get; set; }
    }

    /// <summary>Append-only verified executor evidence; Sequence and DetailId must remain stable on retries.</summary>
    public sealed class OperationJournalDetail
    {
        public string OrganizationId { get; set; }
        public string OperationId { get; set; }
        public string DetailId { get; set; }
        public long Sequence { get; set; }
        public string Phase { get; set; }
        public string Kind { get; set; }
        public string Target { get; set; }
        public string BeforeState { get; set; }
        public string AfterState { get; set; }
        public string EvidenceId { get; set; }
        public DateTimeOffset RecordedAtUtc { get; set; }
    }

    public sealed class OperationJournalPage<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public string ContinuationToken { get; set; }
    }
}
