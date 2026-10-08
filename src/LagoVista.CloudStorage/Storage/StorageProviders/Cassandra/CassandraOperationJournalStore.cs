using Cassandra;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cassandra
{
    /// <summary>
    /// Cassandra journal for server-verified deterministic execution boundaries.
    /// Caller authorization and secret redaction are required before invoking this store.
    /// </summary>
    public sealed class CassandraOperationJournalStore : IOperationJournalStore
    {
        private readonly ICassandraSessionFactory _sessions;
        private readonly SemaphoreSlim _schemaGate = new SemaphoreSlim(1, 1);
        private volatile bool _schemaReady;

        public CassandraOperationJournalStore(ICassandraSessionFactory sessions)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        }

        public async Task<OperationJournalRecord> StartAsync(OperationJournalRecord operation, CancellationToken cancellationToken = default)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            Required(operation.OrganizationId, nameof(operation.OrganizationId));
            Required(operation.OperationId, nameof(operation.OperationId));
            Required(operation.OwnerType, nameof(operation.OwnerType));
            Required(operation.OwnerId, nameof(operation.OwnerId));
            Required(operation.BoundaryType, nameof(operation.BoundaryType));
            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTimeOffset.UtcNow;
            if (operation.StartedAtUtc == default) operation.StartedAtUtc = now;
            operation.UpdatedAtUtc = operation.StartedAtUtc;
            operation.Status = "running";
            var session = await ReadyAsync().ConfigureAwait(false);
            var insert = await session.PrepareAsync(@"INSERT INTO operation_journal
                (organization_id, operation_id, command_id, boundary_type, owner_type, owner_id,
                 workstream_id, workspace_id, fix_workspace_id, status, summary,
                 started_at, updated_at, completed_at, recovery_count, evidence_id)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) IF NOT EXISTS").ConfigureAwait(false);
            var result = await session.ExecuteAsync(insert.Bind(
                operation.OrganizationId, operation.OperationId, operation.CommandId, operation.BoundaryType,
                operation.OwnerType, operation.OwnerId, operation.WorkstreamId, operation.WorkspaceId,
                operation.FixWorkspaceId, operation.Status, operation.Summary, operation.StartedAtUtc,
                operation.UpdatedAtUtc, operation.CompletedAtUtc, operation.RecoveryCount,
                operation.EvidenceId)).ConfigureAwait(false);
            if (!Applied(result))
            {
                var existing = await GetAsync(operation.OrganizationId, operation.OperationId, cancellationToken).ConfigureAwait(false);
                if (existing != null && existing.CommandId == operation.CommandId && existing.BoundaryType == operation.BoundaryType &&
                    existing.OwnerType == operation.OwnerType && existing.OwnerId == operation.OwnerId)
                    return existing;
                throw new InvalidOperationException("Operation id already belongs to a different boundary invocation.");
            }

            foreach (var scope in Scopes(operation))
                await UpsertProjectionAsync(session, operation, scope.Type, scope.Id).ConfigureAwait(false);
            return operation;
        }

        public async Task<OperationJournalRecord> TransitionAsync(string organizationId, string operationId,
            string expectedStatus, string nextStatus, string summary, DateTimeOffset changedAtUtc,
            CancellationToken cancellationToken = default)
        {
            Required(organizationId, nameof(organizationId));
            Required(operationId, nameof(operationId));
            Required(expectedStatus, nameof(expectedStatus));
            Required(nextStatus, nameof(nextStatus));
            cancellationToken.ThrowIfCancellationRequested();
            var current = await GetAsync(organizationId, operationId, cancellationToken).ConfigureAwait(false);
            if (current == null) throw new KeyNotFoundException("Operation not found in the tenant.");
            if (current.Status == nextStatus && current.Summary == summary) return current;
            if (current.Status != expectedStatus)
                throw new InvalidOperationException("Operation status changed concurrently; re-read operation before retry.");
            if (changedAtUtc.Offset != TimeSpan.Zero || changedAtUtc < current.UpdatedAtUtc)
                throw new ArgumentException("Transition time must be UTC and monotonic.", nameof(changedAtUtc));
            if (current.Status == "succeeded" || current.Status == "failed")
                throw new InvalidOperationException("Terminal journal operations cannot be transitioned.");

            var terminal = nextStatus == "succeeded" || nextStatus == "failed";
            var session = await ReadyAsync().ConfigureAwait(false);
            var update = await session.PrepareAsync(@"UPDATE operation_journal
                SET status = ?, summary = ?, updated_at = ?, completed_at = ?
                WHERE organization_id = ? AND operation_id = ? IF status = ?").ConfigureAwait(false);
            var rows = await session.ExecuteAsync(update.Bind(nextStatus, summary, changedAtUtc,
                terminal ? (DateTimeOffset?)changedAtUtc : null, organizationId, operationId, expectedStatus)).ConfigureAwait(false);
            if (!Applied(rows)) throw new InvalidOperationException("Concurrent operation transition rejected by Cassandra.");
            current.Status = nextStatus;
            current.Summary = summary;
            current.UpdatedAtUtc = changedAtUtc;
            current.CompletedAtUtc = terminal ? (DateTimeOffset?)changedAtUtc : null;
            foreach (var scope in Scopes(current))
                await UpsertProjectionAsync(session, current, scope.Type, scope.Id).ConfigureAwait(false);
            return current;
        }

        public async Task<OperationJournalDetail> AppendAsync(OperationJournalDetail detail, CancellationToken cancellationToken = default)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            Required(detail.OrganizationId, nameof(detail.OrganizationId));
            Required(detail.OperationId, nameof(detail.OperationId));
            Required(detail.DetailId, nameof(detail.DetailId));
            if (detail.Sequence < 1) throw new ArgumentOutOfRangeException(nameof(detail.Sequence));
            cancellationToken.ThrowIfCancellationRequested();
            if (await GetAsync(detail.OrganizationId, detail.OperationId, cancellationToken).ConfigureAwait(false) == null)
                throw new KeyNotFoundException("Cannot append evidence for an unknown tenant operation.");
            if (detail.RecordedAtUtc == default) detail.RecordedAtUtc = DateTimeOffset.UtcNow;
            var session = await ReadyAsync().ConfigureAwait(false);
            var statement = await session.PrepareAsync(@"INSERT INTO operation_journal_details
                (organization_id, operation_id, sequence, detail_id, phase, kind, target,
                 before_state, after_state, evidence_id, recorded_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) IF NOT EXISTS").ConfigureAwait(false);
            var rows = await session.ExecuteAsync(statement.Bind(detail.OrganizationId, detail.OperationId,
                detail.Sequence, detail.DetailId, detail.Phase, detail.Kind, detail.Target,
                detail.BeforeState, detail.AfterState, detail.EvidenceId, detail.RecordedAtUtc)).ConfigureAwait(false);
            if (!Applied(rows))
            {
                var existing = await GetDetailAsync(session, detail.OrganizationId, detail.OperationId, detail.Sequence).ConfigureAwait(false);
                if (existing != null && existing.DetailId == detail.DetailId && existing.EvidenceId == detail.EvidenceId &&
                    existing.Phase == detail.Phase && existing.Kind == detail.Kind && existing.Target == detail.Target &&
                    existing.BeforeState == detail.BeforeState && existing.AfterState == detail.AfterState)
                    return existing;
                throw new InvalidOperationException("Evidence sequence collision; retry must preserve original evidence.");
            }
            return detail;
        }

        public async Task<OperationJournalRecord> GetAsync(string organizationId, string operationId, CancellationToken cancellationToken = default)
        {
            Required(organizationId, nameof(organizationId));
            Required(operationId, nameof(operationId));
            cancellationToken.ThrowIfCancellationRequested();
            var session = await ReadyAsync().ConfigureAwait(false);
            var query = await session.PrepareAsync("SELECT * FROM operation_journal WHERE organization_id = ? AND operation_id = ?").ConfigureAwait(false);
            var rows = await session.ExecuteAsync(query.Bind(organizationId, operationId)).ConfigureAwait(false);
            var row = rows.FirstOrDefault();
            return row == null ? null : ReadOperation(row);
        }

        public async Task<OperationJournalPage<OperationJournalRecord>> ListAsync(OperationJournalScope scope,
            int pageSize, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            Required(scope.OrganizationId, nameof(scope.OrganizationId));
            var owner = ScopeKey(scope);
            ValidatePage(pageSize);
            cancellationToken.ThrowIfCancellationRequested();
            var session = await ReadyAsync().ConfigureAwait(false);
            var statement = await session.PrepareAsync(@"SELECT * FROM operation_journal_by_owner
                WHERE organization_id = ? AND scope_type = ? AND scope_id = ?").ConfigureAwait(false);
            var bound = statement.Bind(scope.OrganizationId, owner.Type, owner.Id).SetPageSize(pageSize).SetAutoPage(false);
            ApplyCursor(bound, continuationToken);
            var rows = await session.ExecuteAsync(bound).ConfigureAwait(false);
            return new OperationJournalPage<OperationJournalRecord>
            {
                Items = rows.Select(ReadOperation).ToList(),
                ContinuationToken = Cursor(rows)
            };
        }

        public async Task<OperationJournalPage<OperationJournalDetail>> GetDetailsAsync(string organizationId,
            string operationId, int pageSize, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            ValidatePage(pageSize);
            if (await GetAsync(organizationId, operationId, cancellationToken).ConfigureAwait(false) == null)
                throw new KeyNotFoundException("Operation not found in the tenant.");
            var session = await ReadyAsync().ConfigureAwait(false);
            var statement = await session.PrepareAsync(@"SELECT * FROM operation_journal_details
                WHERE organization_id = ? AND operation_id = ?").ConfigureAwait(false);
            var bound = statement.Bind(organizationId, operationId).SetPageSize(pageSize).SetAutoPage(false);
            ApplyCursor(bound, continuationToken);
            var rows = await session.ExecuteAsync(bound).ConfigureAwait(false);
            return new OperationJournalPage<OperationJournalDetail>
            {
                Items = rows.Select(ReadDetail).ToList(),
                ContinuationToken = Cursor(rows)
            };
        }

        private async Task<OperationJournalDetail> GetDetailAsync(ISession session, string org, string operation, long sequence)
        {
            var query = await session.PrepareAsync(@"SELECT * FROM operation_journal_details
                WHERE organization_id = ? AND operation_id = ? AND sequence = ?").ConfigureAwait(false);
            var row = (await session.ExecuteAsync(query.Bind(org, operation, sequence)).ConfigureAwait(false)).FirstOrDefault();
            return row == null ? null : ReadDetail(row);
        }

        private async Task UpsertProjectionAsync(ISession session, OperationJournalRecord operation, string scopeType, string scopeId)
        {
            var insert = await session.PrepareAsync(@"INSERT INTO operation_journal_by_owner
                (organization_id, scope_type, scope_id, started_at, operation_id, command_id,
                 boundary_type, owner_type, owner_id, workstream_id, workspace_id,
                 fix_workspace_id, status, summary, updated_at, completed_at, recovery_count, evidence_id)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)").ConfigureAwait(false);
            await session.ExecuteAsync(insert.Bind(operation.OrganizationId, scopeType, scopeId,
                operation.StartedAtUtc, operation.OperationId, operation.CommandId, operation.BoundaryType,
                operation.OwnerType, operation.OwnerId, operation.WorkstreamId, operation.WorkspaceId,
                operation.FixWorkspaceId, operation.Status, operation.Summary, operation.UpdatedAtUtc,
                operation.CompletedAtUtc, operation.RecoveryCount, operation.EvidenceId)).ConfigureAwait(false);
        }

        private static IEnumerable<(string Type, string Id)> Scopes(OperationJournalRecord record)
        {
            yield return ("all", "all");
            yield return (record.OwnerType, record.OwnerId);
            if (!String.IsNullOrWhiteSpace(record.WorkstreamId)) yield return ("workstream", record.WorkstreamId);
            if (!String.IsNullOrWhiteSpace(record.WorkspaceId)) yield return ("workspace", record.WorkspaceId);
            if (!String.IsNullOrWhiteSpace(record.FixWorkspaceId)) yield return ("fix-workspace", record.FixWorkspaceId);
        }

        private static (string Type, string Id) ScopeKey(OperationJournalScope scope)
        {
            switch (scope.ScopeType?.Trim().ToLowerInvariant())
            {
                case "all": return ("all", "all");
                case "workstream": Required(scope.WorkstreamId, nameof(scope.WorkstreamId)); return ("workstream", scope.WorkstreamId);
                case "workspace": Required(scope.WorkspaceId, nameof(scope.WorkspaceId)); return ("workspace", scope.WorkspaceId);
                case "fix-workspace": Required(scope.FixWorkspaceId, nameof(scope.FixWorkspaceId)); return ("fix-workspace", scope.FixWorkspaceId);
                default: throw new ArgumentException("Unsupported owner scope.", nameof(scope));
            }
        }

        private async Task<ISession> ReadyAsync()
        {
            var session = await _sessions.GetSessionAsync().ConfigureAwait(false);
            if (_schemaReady) return session;
            await _schemaGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_schemaReady)
                {
                    await session.ExecuteAsync(new SimpleStatement(@"CREATE TABLE IF NOT EXISTS operation_journal (
                        organization_id text, operation_id text, command_id text, boundary_type text,
                        owner_type text, owner_id text, workstream_id text, workspace_id text,
                        fix_workspace_id text, status text, summary text, started_at timestamp,
                        updated_at timestamp, completed_at timestamp, recovery_count int,
                        evidence_id text, PRIMARY KEY ((organization_id), operation_id))")).ConfigureAwait(false);
                    await session.ExecuteAsync(new SimpleStatement(@"CREATE TABLE IF NOT EXISTS operation_journal_by_owner (
                        organization_id text, scope_type text, scope_id text, started_at timestamp,
                        operation_id text, command_id text, boundary_type text, owner_type text,
                        owner_id text, workstream_id text, workspace_id text, fix_workspace_id text,
                        status text, summary text, updated_at timestamp, completed_at timestamp,
                        recovery_count int, evidence_id text,
                        PRIMARY KEY ((organization_id, scope_type, scope_id), started_at, operation_id))
                        WITH CLUSTERING ORDER BY (started_at DESC, operation_id ASC)")).ConfigureAwait(false);
                    await session.ExecuteAsync(new SimpleStatement(@"CREATE TABLE IF NOT EXISTS operation_journal_details (
                        organization_id text, operation_id text, sequence bigint, detail_id text,
                        phase text, kind text, target text, before_state text, after_state text,
                        evidence_id text, recorded_at timestamp,
                        PRIMARY KEY ((organization_id, operation_id), sequence))
                        WITH CLUSTERING ORDER BY (sequence ASC)")).ConfigureAwait(false);
                    _schemaReady = true;
                }
            }
            finally { _schemaGate.Release(); }
            return session;
        }

        private static bool Applied(RowSet rows) => rows.FirstOrDefault()?.GetValue<bool>("[applied]") == true;
        private static void Required(string value, string name)
        {
            if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required journal identity is missing.", name);
        }
        private static void ValidatePage(int size)
        {
            if (size < 1 || size > 200) throw new ArgumentOutOfRangeException(nameof(size), "Page size must be 1-200.");
        }
        private static void ApplyCursor(IStatement statement, string cursor)
        {
            if (String.IsNullOrEmpty(cursor)) return;
            try { statement.SetPagingState(Convert.FromBase64String(cursor)); }
            catch (FormatException ex) { throw new ArgumentException("Invalid journal cursor.", nameof(cursor), ex); }
        }
        private static string Cursor(RowSet rows) => rows.PagingState == null || rows.PagingState.Length == 0
            ? null : Convert.ToBase64String(rows.PagingState);
        private static string GetString(Row row, string key) => row.IsNull(key) ? null : row.GetValue<string>(key);
        private static DateTimeOffset? GetTime(Row row, string key) => row.IsNull(key) ? (DateTimeOffset?)null : row.GetValue<DateTimeOffset>(key);
        private static OperationJournalRecord ReadOperation(Row row) => new OperationJournalRecord
        {
            OrganizationId = GetString(row, "organization_id"), OperationId = GetString(row, "operation_id"),
            CommandId = GetString(row, "command_id"), BoundaryType = GetString(row, "boundary_type"),
            OwnerType = GetString(row, "owner_type"), OwnerId = GetString(row, "owner_id"),
            WorkstreamId = GetString(row, "workstream_id"), WorkspaceId = GetString(row, "workspace_id"),
            FixWorkspaceId = GetString(row, "fix_workspace_id"), Status = GetString(row, "status"),
            Summary = GetString(row, "summary"), StartedAtUtc = row.GetValue<DateTimeOffset>("started_at"),
            UpdatedAtUtc = row.GetValue<DateTimeOffset>("updated_at"), CompletedAtUtc = GetTime(row, "completed_at"),
            RecoveryCount = row.IsNull("recovery_count") ? 0 : row.GetValue<int>("recovery_count"),
            EvidenceId = GetString(row, "evidence_id")
        };
        private static OperationJournalDetail ReadDetail(Row row) => new OperationJournalDetail
        {
            OrganizationId = GetString(row, "organization_id"), OperationId = GetString(row, "operation_id"),
            DetailId = GetString(row, "detail_id"), Sequence = row.GetValue<long>("sequence"),
            Phase = GetString(row, "phase"), Kind = GetString(row, "kind"), Target = GetString(row, "target"),
            BeforeState = GetString(row, "before_state"), AfterState = GetString(row, "after_state"),
            EvidenceId = GetString(row, "evidence_id"), RecordedAtUtc = row.GetValue<DateTimeOffset>("recorded_at")
        };
    }
}
