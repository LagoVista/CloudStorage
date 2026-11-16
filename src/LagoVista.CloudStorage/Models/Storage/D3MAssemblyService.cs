using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>Read-only publication lookup. Drafts never drive release assembly.</summary>
    public interface ID3MPublishedDefinitionReader
    {
        Task<D3MPublishedDefinitionRecord> GetPublishedAsync(
            string typeKey, string organizationId, CancellationToken cancellationToken = default);
    }

    /// <summary>Identity of a source record. An adapter may resolve a key to a canonical ID.</summary>
    public sealed class D3MRecordLocator
    {
        public string OrganizationId { get; set; }
        public string TypeKey { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }
    }

    /// <summary>
    /// A trusted domain adapter's approved snapshot. Snapshot is for adapter use only;
    /// it must never be placed on the result graph or sent in a release preview.
    /// </summary>
    public sealed class D3MApprovedSourceRecord
    {
        public string OrganizationId { get; set; }
        public string TypeKey { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string StableRevision { get; set; }
        public bool IsApproved { get; set; }
        public object Snapshot { get; set; }
    }

    public sealed class D3MSourceReference
    {
        public string Id { get; set; }
        public string Key { get; set; }
    }

    /// <summary>
    /// Code-owned source adapter for one registered storage class. The host supplies
    /// typed source readers and explicit relationship accessors; D3M never interprets
    /// arbitrary CLR reflection, queries, or executable database-authored rules.
    /// </summary>
    public interface ID3MApprovedSourceAdapter
    {
        D3MStorageKind Kind { get; }
        Task<D3MApprovedSourceRecord> ReadApprovedAsync(
            D3MRecordLocator locator, D3MTypeRegistration registration,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<D3MSourceReference>> ReadReferencesAsync(
            D3MApprovedSourceRecord source, D3MRegisteredRelationship relationship,
            CancellationToken cancellationToken = default);
        Task<bool> EvaluateConditionAsync(
            D3MApprovedSourceRecord source, D3MRegisteredRelationship relationship,
            CancellationToken cancellationToken = default);
    }

    public sealed class D3MAssemblyNode
    {
        public string TypeKey { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public D3MStorageKind StorageKind { get; set; }
        public string ApplicationDataCollection { get; set; }
        public string StableSourceRevision { get; set; }
        public int PublishedMapRevision { get; set; }
        public string PublishedMapId { get; set; }
    }

    public sealed class D3MAssemblyEdge
    {
        public string FromTypeKey { get; set; }
        public string FromId { get; set; }
        public string ToTypeKey { get; set; }
        public string ToId { get; set; }
        public string ToKey { get; set; }
        public string PropertyPath { get; set; }
        public D3MPresence Presence { get; set; }
        public D3MRelationshipRole Role { get; set; }
        public D3MVersionRequirement VersionRequirement { get; set; }
        public string BindingKey { get; set; }
    }

    /// <summary>Read-only metadata proof; neither approval nor an executable transfer plan.</summary>
    public sealed class D3MAssemblyResult
    {
        public string OrganizationId { get; set; }
        public string RootTypeKey { get; set; }
        public string RootId { get; set; }
        public List<D3MAssemblyNode> OrderedNodes { get; } = new List<D3MAssemblyNode>();
        public List<D3MAssemblyEdge> Edges { get; } = new List<D3MAssemblyEdge>();
        public List<string> Blockers { get; } = new List<string>();
        public bool GraphComplete => Blockers.Count == 0;
    }

    /// <summary>
    /// Generic read-only traversal of published, version-pinned D3Ms.
    /// Nodes are dependency-first and deduplicated by organization/type/canonical ID.
    /// Navigation-only edges remain visible but never affect prerequisite ordering.
    /// </summary>
    public sealed class D3MAssemblyService
    {
        private const int MaxNodes = 512;
        private const int MaxEdges = 4096;
        private const int MaxDepth = 64;
        private readonly ID3MPublishedDefinitionReader _definitions;
        private readonly D3MTypeRegistry _registry;
        private readonly D3MValidator _validator;
        private readonly IReadOnlyDictionary<D3MStorageKind, ID3MApprovedSourceAdapter> _adapters;

        public D3MAssemblyService(ID3MPublishedDefinitionReader definitions, D3MTypeRegistry registry,
            IEnumerable<ID3MApprovedSourceAdapter> adapters)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _validator = new D3MValidator(registry);
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            var available = new Dictionary<D3MStorageKind, ID3MApprovedSourceAdapter>();
            foreach (var adapter in adapters)
            {
                if (adapter == null || !Enum.IsDefined(typeof(D3MStorageKind), adapter.Kind) ||
                    available.ContainsKey(adapter.Kind))
                    throw new ArgumentException("D3M adapters must have distinct valid storage classes.", nameof(adapters));
                available.Add(adapter.Kind, adapter);
            }
            _adapters = available;
        }

        public async Task<D3MAssemblyResult> AssembleAsync(string organizationId, string rootTypeKey,
            string rootId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(organizationId))
                throw new ArgumentException("An authenticated organization scope is required.", nameof(organizationId));
            if (String.IsNullOrWhiteSpace(rootTypeKey))
                throw new ArgumentException("A registered root type is required.", nameof(rootTypeKey));
            if (String.IsNullOrWhiteSpace(rootId))
                throw new ArgumentException("A root record ID is required.", nameof(rootId));

            var result = new D3MAssemblyResult
            {
                OrganizationId = organizationId, RootTypeKey = rootTypeKey, RootId = rootId
            };
            var states = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            var maps = new Dictionary<string, D3MPublishedDefinitionRecord>(StringComparer.Ordinal);
            var blockerSet = new HashSet<string>(StringComparer.Ordinal);
            var stack = new List<string>();
            void Block(string message)
            {
                if (blockerSet.Add(message)) result.Blockers.Add(message);
            }
            string ScopedKey(string type, string id) => organizationId + ":" + type + ":" + id;

            async Task<D3MPublishedDefinitionRecord> GetMapAsync(string typeKey, bool root)
            {
                if (maps.TryGetValue(typeKey, out var cached)) return cached;
                if (!_registry.TryGet(typeKey, out var registration))
                {
                    Block("Unregistered D3M type: " + typeKey);
                    return null;
                }
                var published = await _definitions.GetPublishedAsync(
                    typeKey, organizationId, cancellationToken).ConfigureAwait(false);
                if (published == null)
                {
                    Block("Missing active published D3M for " + typeKey + ". A draft is not sufficient.");
                    return null;
                }
                if (!String.Equals(published.TypeKey, typeKey, StringComparison.Ordinal) ||
                    !String.Equals(published.Organization?.Id, organizationId, StringComparison.OrdinalIgnoreCase) ||
                    published.Revision < 1 || published.SchemaVersion != 1 ||
                    published.Id.Value != D3MIdentity.Published(typeKey, published.Revision).Value ||
                    String.IsNullOrWhiteSpace(published.PublishedBy))
                {
                    Block("Invalid publication identity, schema or provenance for " + typeKey + ".");
                    return null;
                }
                var draftShape = new D3MDefinitionRecord
                {
                    Id = D3MIdentity.Definition(typeKey), Organization = published.Organization,
                    TypeKey = published.TypeKey, Name = published.Name,
                    StorageKind = published.StorageKind,
                    ApplicationDataCollection = published.ApplicationDataCollection,
                    SchemaVersion = published.SchemaVersion, DraftRevision = published.Revision,
                    TopLevelSelectable = published.TopLevelSelectable,
                    ExplicitLeaf = published.ExplicitLeaf, Dependencies = published.Dependencies
                };
                var validation = _validator.Validate(draftShape);
                if (!validation.IsValid)
                {
                    Block("Published D3M differs from the trusted registered type " + typeKey + ": " +
                        String.Join(", ", validation.Issues.Select(issue => issue.Code)));
                    return null;
                }
                if (root && !published.TopLevelSelectable)
                {
                    Block("D3M type is not approved as a release root: " + typeKey);
                    return null;
                }
                maps.Add(typeKey, published);
                return published;
            }

            async Task VisitAsync(D3MRecordLocator locator, int depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var type = locator.TypeKey;
                if (depth > MaxDepth)
                {
                    Block("Maximum D3M dependency depth exceeded at " + type + ".");
                    return;
                }
                if (!_registry.TryGet(type, out var registration))
                {
                    Block("Unregistered referenced D3M type: " + type);
                    return;
                }
                var map = await GetMapAsync(type, depth == 0).ConfigureAwait(false);
                if (map == null) return;
                if (!_adapters.TryGetValue(registration.StorageKind, out var adapter))
                {
                    Block("No approved source adapter registered for " + registration.StorageKind + " (" + type + ").");
                    return;
                }
                var source = await adapter.ReadApprovedAsync(locator, registration, cancellationToken)
                    .ConfigureAwait(false);
                if (source == null || !source.IsApproved || String.IsNullOrWhiteSpace(source.StableRevision) ||
                    !String.Equals(source.TypeKey, type, StringComparison.Ordinal) ||
                    !String.Equals(source.OrganizationId, organizationId, StringComparison.OrdinalIgnoreCase) ||
                    String.IsNullOrWhiteSpace(source.Id) ||
                    (!String.IsNullOrWhiteSpace(locator.Id) &&
                     !String.Equals(locator.Id, source.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    Block("Missing or unapproved stable source snapshot for " + type + ":" +
                        (locator.Id ?? locator.Key) + ".");
                    return;
                }
                var identity = ScopedKey(type, source.Id);
                if (states.TryGetValue(identity, out var state))
                {
                    if (state == 1)
                        Block("Prerequisite cycle: " + String.Join(" -> ", stack.Concat(new[] { identity })));
                    return;
                }
                if (states.Count >= MaxNodes)
                {
                    Block("D3M closure exceeds maximum source records (" + MaxNodes + ").");
                    return;
                }
                states.Add(identity, 1);
                stack.Add(identity);
                var node = new D3MAssemblyNode
                {
                    TypeKey = type, Id = source.Id, Key = source.Key,
                    Name = source.Name, StorageKind = map.StorageKind,
                    ApplicationDataCollection = map.ApplicationDataCollection,
                    StableSourceRevision = source.StableRevision,
                    PublishedMapId = map.Id.Value, PublishedMapRevision = map.Revision
                };
                foreach (var rule in map.Dependencies ?? new List<D3MDependencyRule>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Published rules were validated against these trusted accessors.
                    if (!registration.TryGetRelationship(rule.PropertyPath, out var relationship))
                    {
                        Block("Missing registered accessor " + type + "." + rule.PropertyPath);
                        continue;
                    }
                    if (rule.Presence == D3MPresence.Conditional &&
                        !await adapter.EvaluateConditionAsync(source, relationship, cancellationToken)
                            .ConfigureAwait(false))
                        continue;

                    var references = await adapter.ReadReferencesAsync(source, relationship, cancellationToken)
                        .ConfigureAwait(false) ?? Array.Empty<D3MSourceReference>();
                    var distinct = references.Where(reference => reference != null)
                        .GroupBy(reference => reference.Id ?? "key:" + reference.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .OrderBy(reference => reference.Id ?? reference.Key, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (distinct.Count == 0 && rule.Role == D3MRelationshipRole.Prerequisite &&
                        rule.Presence != D3MPresence.Optional)
                        Block("Missing required dependency " + type + ":" + source.Id + "." + rule.PropertyPath);
                    if (rule.Cardinality == D3MCardinality.One && distinct.Count > 1)
                        Block("Single-valued dependency returned multiple references: " + type + "." + rule.PropertyPath);
                    foreach (var reference in distinct)
                    {
                        if (String.IsNullOrWhiteSpace(reference.Id) && String.IsNullOrWhiteSpace(reference.Key))
                        {
                            Block("Unresolved reference identity: " + type + "." + rule.PropertyPath);
                            continue;
                        }
                        if (result.Edges.Count >= MaxEdges)
                        {
                            Block("D3M closure exceeds maximum dependency edges (" + MaxEdges + ").");
                            break;
                        }
                        result.Edges.Add(new D3MAssemblyEdge
                        {
                            FromTypeKey = type, FromId = source.Id,
                            ToTypeKey = rule.TargetTypeKey, ToId = reference.Id, ToKey = reference.Key,
                            PropertyPath = rule.PropertyPath, Role = rule.Role,
                            Presence = rule.Presence, VersionRequirement = rule.VersionRequirement,
                            BindingKey = rule.BindingKey
                        });
                        if (rule.Role == D3MRelationshipRole.Prerequisite)
                            await VisitAsync(new D3MRecordLocator
                            {
                                OrganizationId = organizationId, TypeKey = rule.TargetTypeKey,
                                Id = reference.Id, Key = reference.Key
                            }, depth + 1).ConfigureAwait(false);
                    }
                }
                stack.RemoveAt(stack.Count - 1);
                states[identity] = 2;
                result.OrderedNodes.Add(node);
            }

            await VisitAsync(new D3MRecordLocator
            {
                OrganizationId = organizationId, TypeKey = rootTypeKey, Id = rootId
            }, 0).ConfigureAwait(false);
            return result;
        }
    }
}
