using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Controlled authoring and publication over the shared ApplicationData capability.
    /// Authentication and the authoritative organization are supplied by the hosting API/MCP
    /// boundary; this service never accepts a caller-selected storage collection or CLR type.
    /// </summary>
    public sealed class D3MDefinitionService
    {
        private readonly IApplicationDataStore _store;
        private readonly D3MTypeRegistry _registry;
        private readonly D3MValidator _validator;

        public D3MDefinitionService(IApplicationDataStore store, D3MTypeRegistry registry)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _validator = new D3MValidator(registry);
        }

        public D3MValidationResult Validate(D3MDefinitionRecord draft) => _validator.Validate(draft);
 
        /// <summary>Organization-scoped, paged authoring catalog.</summary>
        public Task<StoragePageResult<D3MDefinitionRecord>> ListDraftsAsync(
            string organizationId, StoragePageRequest page = null, CancellationToken cancellationToken = default)
        {
            var query = new StorageQuery<D3MDefinitionRecord>()
                .Where(record => record.Organization.Id, StorageFilterOperator.Equal, RequiredOrganization(organizationId))
                .OrderBy(record => record.TypeKey)
                .WithPage(page ?? new StoragePageRequest());
            return _store.QueryAsync(query, cancellationToken);
        }

        /// <summary>Organization-scoped, paged immutable history for one registered type.</summary>
        public Task<StoragePageResult<D3MPublishedDefinitionRecord>> ListRevisionsAsync(
            string typeKey, string organizationId, StoragePageRequest page = null,
            CancellationToken cancellationToken = default)
        {
            if (!_registry.Contains(typeKey)) throw new InvalidOperationException("Unknown registered D3M type.");
            var query = new StorageQuery<D3MPublishedDefinitionRecord>()
                .Where(record => record.Organization.Id, StorageFilterOperator.Equal, RequiredOrganization(organizationId))
                .Where(record => record.TypeKey, StorageFilterOperator.Equal, typeKey)
                .OrderBy(record => record.Revision, StorageSortDirection.Descending)
                .WithPage(page ?? new StoragePageRequest());
            return _store.QueryAsync(query, cancellationToken);
        }

        public Task<VersionedApplicationDataRecord<D3MDefinitionRecord>> GetDraftAsync(
            string typeKey, string organizationId, CancellationToken cancellationToken = default)
            => _store.GetVersionedAsync<D3MDefinitionRecord>(
                new StorageKey(D3MIdentity.Definition(typeKey).Value, RequiredOrganization(organizationId)),
                cancellationToken);

        public Task<D3MPublishedDefinitionRecord> GetRevisionAsync(
            string typeKey, string organizationId, int revision, CancellationToken cancellationToken = default)
            => _store.GetAsync<D3MPublishedDefinitionRecord>(
                new StorageKey(D3MIdentity.Published(typeKey, revision).Value, RequiredOrganization(organizationId)),
                cancellationToken);

        /// <summary>
        /// Only the latest published revision is active. An older pinned immutable revision
        /// remains retrievable through GetRevisionAsync after editing, disabling, or retiring.
        /// </summary>
        public async Task<D3MPublishedDefinitionRecord> GetPublishedAsync(
            string typeKey, string organizationId, CancellationToken cancellationToken = default)
        {
            var draft = await GetDraftAsync(typeKey, organizationId, cancellationToken).ConfigureAwait(false);
            if (draft?.Record?.PublishedRevision == null || !draft.Record.Enabled || draft.Record.Retired)
                return null;
            var published = await GetRevisionAsync(typeKey, organizationId,
                draft.Record.PublishedRevision.Value, cancellationToken).ConfigureAwait(false);
            if (published == null)
                throw new InvalidOperationException("Published D3M pointer has no immutable revision. Promotion must be blocked.");
            return published;
        }

        /// <summary>Create a draft. Drafts need not be complete; publication has strict validation.</summary>
        public Task CreateDraftAsync(D3MDefinitionRecord draft, CancellationToken cancellationToken = default)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            if (!_registry.Contains(draft.TypeKey))
                throw new InvalidOperationException("Unknown registered D3M type.");
            RequiredOrganization(draft.Organization?.Id);
            draft.Id = D3MIdentity.Definition(draft.TypeKey);
            draft.SchemaVersion = 1;
            draft.DraftRevision = 1;
            draft.PublishedRevision = null;
            draft.PublishedBy = null;
            draft.PublishedUtc = null;
            return _store.InsertAsync(draft, cancellationToken);
        }

        /// <summary>
        /// CAS draft update: retain identity, organization, and publication provenance from
        /// the stored record. Never allow a supplied draft to overwrite a published pointer.
        /// </summary>
        public async Task<ApplicationDataConcurrencyToken> UpdateDraftIfVersionAsync(
            D3MDefinitionRecord proposed, ApplicationDataConcurrencyToken expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (proposed == null) throw new ArgumentNullException(nameof(proposed));
            if (expectedVersion == null) throw new ArgumentNullException(nameof(expectedVersion));
            if (!_registry.Contains(proposed.TypeKey))
                throw new InvalidOperationException("Unknown registered D3M type.");
            var organizationId = RequiredOrganization(proposed.Organization?.Id);
            var current = await GetDraftAsync(proposed.TypeKey, organizationId, cancellationToken).ConfigureAwait(false);
            if (current == null)
                throw new InvalidOperationException("Cannot update a missing D3M draft.");
            if (!current.ConcurrencyToken.Equals(expectedVersion))
                throw new InvalidOperationException("D3M draft has changed; reload before editing.");

            var draft = current.Record;
            if (draft.Retired)
                throw new InvalidOperationException("A retired D3M cannot be edited.");
            draft.Name = proposed.Name;
            draft.Description = proposed.Description;
            draft.StorageKind = proposed.StorageKind;
            draft.ApplicationDataCollection = proposed.ApplicationDataCollection;
            draft.TopLevelSelectable = proposed.TopLevelSelectable;
            draft.ExplicitLeaf = proposed.ExplicitLeaf;
            draft.Enabled = proposed.Enabled;
            draft.Dependencies = CopyRules(proposed.Dependencies);
            draft.DraftRevision = checked(draft.DraftRevision + 1);
            return await _store.UpdateIfVersionAsync(draft, expectedVersion, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Insert immutable snapshot before moving the published pointer with CAS. A CAS
        /// collision can leave an unreferenced snapshot, but never a pointer to missing data.
        /// A subsequent attempt must reload the draft and use a new revision after editing.
        /// </summary>
        public async Task<D3MPublishedDefinitionRecord> PublishAsync(string typeKey,
            string organizationId, string approvedBy, ApplicationDataConcurrencyToken expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(approvedBy)) throw new ArgumentException("Approval actor is required.", nameof(approvedBy));
            if (expectedVersion == null) throw new ArgumentNullException(nameof(expectedVersion));
            var current = await GetDraftAsync(typeKey, organizationId, cancellationToken).ConfigureAwait(false);
            if (current == null) throw new InvalidOperationException("D3M draft was not found.");
            if (!current.ConcurrencyToken.Equals(expectedVersion))
                throw new InvalidOperationException("D3M draft has changed; review and validate the current revision.");
            var draft = current.Record;
            var validation = _validator.Validate(draft);
            if (!validation.IsValid)
                throw new InvalidOperationException("D3M publication blocked: " +
                    String.Join("; ", validation.Issues.Select(issue => issue.Code + " at " + issue.Path)));

            if (draft.PublishedRevision == draft.DraftRevision)
            {
                var existing = await GetRevisionAsync(typeKey, organizationId, draft.DraftRevision,
                    cancellationToken).ConfigureAwait(false);
                if (existing == null)
                    throw new InvalidOperationException("D3M published revision is missing.");
                return existing;
            }

            var now = DateTime.UtcNow;
            var published = new D3MPublishedDefinitionRecord
            {
                Id = D3MIdentity.Published(typeKey, draft.DraftRevision),
                Organization = draft.Organization,
                TypeKey = draft.TypeKey,
                Name = draft.Name,
                Description = draft.Description,
                StorageKind = draft.StorageKind,
                ApplicationDataCollection = draft.ApplicationDataCollection,
                SchemaVersion = draft.SchemaVersion,
                Revision = draft.DraftRevision,
                PublishedBy = approvedBy.Trim(),
                PublishedUtc = now,
                TopLevelSelectable = draft.TopLevelSelectable,
                ExplicitLeaf = draft.ExplicitLeaf,
                Dependencies = CopyRules(draft.Dependencies)
            };
            await _store.InsertAsync(published, cancellationToken).ConfigureAwait(false);
            draft.PublishedRevision = draft.DraftRevision;
            draft.PublishedBy = approvedBy.Trim();
            draft.PublishedUtc = now;
            await _store.UpdateIfVersionAsync(draft, expectedVersion, cancellationToken).ConfigureAwait(false);
            return published;
        }

        /// <summary>
        /// Retirement changes only the active pointer state, never an immutable revision.
        /// The caller must authorize the organization and operation.
        /// </summary>
        public async Task<ApplicationDataConcurrencyToken> RetireIfVersionAsync(
            string typeKey, string organizationId, ApplicationDataConcurrencyToken expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (expectedVersion == null) throw new ArgumentNullException(nameof(expectedVersion));
            var current = await GetDraftAsync(typeKey, organizationId, cancellationToken).ConfigureAwait(false);
            if (current == null) throw new InvalidOperationException("D3M draft was not found.");
            if (!current.ConcurrencyToken.Equals(expectedVersion))
                throw new InvalidOperationException("D3M draft has changed; reload before retirement.");
            current.Record.Retired = true;
            current.Record.Enabled = false;
            return await _store.UpdateIfVersionAsync(current.Record, expectedVersion, cancellationToken).ConfigureAwait(false);
        }

        private static List<D3MDependencyRule> CopyRules(IEnumerable<D3MDependencyRule> rules)
            => (rules ?? Enumerable.Empty<D3MDependencyRule>()).Select(rule => rule == null ? null :
                new D3MDependencyRule
                {
                    PropertyPath = rule.PropertyPath,
                    TargetTypeKey = rule.TargetTypeKey,
                    Cardinality = rule.Cardinality,
                    Presence = rule.Presence,
                    Role = rule.Role,
                    VersionRequirement = rule.VersionRequirement,
                    ConditionKey = rule.ConditionKey,
                    BindingKey = rule.BindingKey
                }).ToList();

        private static string RequiredOrganization(string organizationId)
        {
            if (String.IsNullOrWhiteSpace(organizationId))
                throw new ArgumentException("Authoritative organization is required.", nameof(organizationId));
            return organizationId;
        }
    }
}
