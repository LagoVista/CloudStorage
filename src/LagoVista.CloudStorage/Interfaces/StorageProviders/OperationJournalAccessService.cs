using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>Authorized application-service boundary for the Operation Journal HTTP API.
    /// The host constructs this identity from authenticated claims, never request JSON.</summary>
    public sealed class OperationJournalPrincipal
    {
        public string OrganizationId { get; set; }
        public bool InternalDeterministicExecutor { get; set; }
        public bool CanReadAllOperations { get; set; }
        public IReadOnlyCollection<string> WorkstreamIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> WorkspaceIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> FixWorkspaceIds { get; set; } = Array.Empty<string>();
    }

    /// <summary>Read/write facade used by the platform API, without exposing storage authorization bypasses.</summary>
    public sealed class OperationJournalAccessService
    {
        private readonly IOperationJournalStore _store;
        public OperationJournalAccessService(IOperationJournalStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Task<OperationJournalRecord> StartAsync(OperationJournalPrincipal caller,
            OperationJournalRecord record, CancellationToken token = default)
        {
            RequireExecutor(caller);
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireTenant(caller, record.OrganizationId);
            return _store.StartAsync(record, token);
        }

        public Task<OperationJournalRecord> TransitionAsync(OperationJournalPrincipal caller,
            string operationId, string expected, string next, string summary, DateTimeOffset timestamp,
            CancellationToken token = default)
        {
            RequireExecutor(caller);
            return _store.TransitionAsync(caller.OrganizationId, operationId, expected, next, summary, timestamp, token);
        }

        public Task<OperationJournalRecord> RecoverAsync(OperationJournalPrincipal caller,
            string operationId, string expected, string summary, DateTimeOffset timestamp,
            CancellationToken token = default)
        {
            RequireExecutor(caller);
            return _store.RecoverAsync(caller.OrganizationId, operationId, expected, summary, timestamp, token);
        }

        public Task<OperationJournalDetail> AppendAsync(OperationJournalPrincipal caller,
            OperationJournalDetail detail, CancellationToken token = default)
        {
            RequireExecutor(caller);
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            RequireTenant(caller, detail.OrganizationId);
            return _store.AppendAsync(detail, token);
        }

        public async Task<OperationJournalRecord> GetAsync(OperationJournalPrincipal caller,
            string operationId, CancellationToken token = default)
        {
            RequirePrincipal(caller);
            var record = await _store.GetAsync(caller.OrganizationId, operationId, token).ConfigureAwait(false);
            if (record != null) RequireOwner(caller, record);
            return record;
        }

        public Task<OperationJournalPage<OperationJournalRecord>> ListAsync(OperationJournalPrincipal caller,
            OperationJournalScope scope, int pageSize, string cursor = null, CancellationToken token = default)
        {
            RequirePrincipal(caller);
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            RequireTenant(caller, scope.OrganizationId);
            RequireScope(caller, scope);
            return _store.ListAsync(scope, pageSize, cursor, token);
        }

        public async Task<OperationJournalPage<OperationJournalDetail>> GetDetailsAsync(OperationJournalPrincipal caller,
            string operationId, int pageSize, string cursor = null, CancellationToken token = default)
        {
            // Always resolve and authorize the canonical Operation before revealing a detail partition.
            var record = await GetAsync(caller, operationId, token).ConfigureAwait(false);
            if (record == null) throw new KeyNotFoundException("Operation not found.");
            return await _store.GetDetailsAsync(caller.OrganizationId, operationId, pageSize, cursor, token).ConfigureAwait(false);
        }

        private static void RequirePrincipal(OperationJournalPrincipal caller)
        {
            if (caller == null || String.IsNullOrWhiteSpace(caller.OrganizationId))
                throw new UnauthorizedAccessException("Authenticated tenant identity required.");
        }

        private static void RequireExecutor(OperationJournalPrincipal caller)
        {
            RequirePrincipal(caller);
            if (!caller.InternalDeterministicExecutor)
                throw new UnauthorizedAccessException("Journal writes require internal deterministic executor authority.");
        }

        private static void RequireTenant(OperationJournalPrincipal caller, string organizationId)
        {
            if (!String.Equals(caller.OrganizationId, organizationId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Operation journal tenant mismatch.");
        }

        private static bool Contains(IReadOnlyCollection<string> ids, string id)
        {
            if (ids == null || String.IsNullOrWhiteSpace(id)) return false;
            foreach (var authorized in ids)
                if (String.Equals(authorized, id, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void RequireOwner(OperationJournalPrincipal caller, OperationJournalRecord record)
        {
            if (caller.CanReadAllOperations) return;
            if (Contains(caller.WorkstreamIds, record.WorkstreamId) ||
                Contains(caller.WorkspaceIds, record.WorkspaceId) ||
                Contains(caller.FixWorkspaceIds, record.FixWorkspaceId))
                return;
            throw new UnauthorizedAccessException("Operation is not authorized for this owner.");
        }

        private static void RequireScope(OperationJournalPrincipal caller, OperationJournalScope scope)
        {
            if (caller.CanReadAllOperations) return;
            switch (scope.ScopeType?.Trim().ToLowerInvariant())
            {
                case "workstream":
                    if (Contains(caller.WorkstreamIds, scope.WorkstreamId)) return;
                    break;
                case "workspace":
                    if (Contains(caller.WorkspaceIds, scope.WorkspaceId)) return;
                    break;
                case "fix-workspace":
                    if (Contains(caller.FixWorkspaceIds, scope.FixWorkspaceId)) return;
                    break;
            }
            throw new UnauthorizedAccessException("Operation history scope is not authorized.");
        }
    }
}
