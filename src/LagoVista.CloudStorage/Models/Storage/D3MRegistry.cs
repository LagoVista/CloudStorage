using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Code-owned allowlist entry. Register actual domain accessors/handlers before permitting
    /// authors to reference them. A persisted D3M never activates a CLR type or runs a query.
    /// </summary>
    public sealed class D3MRegisteredRelationship
    {
        public string PropertyPath { get; }
        public string TargetTypeKey { get; }
        public D3MCardinality Cardinality { get; }
        public D3MRelationshipRole Role { get; }
        public string ConditionKey { get; }
        public string BindingKey { get; }
        public bool SupportsCompatibleStableTarget { get; }

        internal D3MRegisteredRelationship(string path, string targetTypeKey,
            D3MCardinality cardinality, D3MRelationshipRole role,
            string conditionKey, string bindingKey, bool supportsCompatibleStableTarget)
        {
            PropertyPath = path;
            TargetTypeKey = targetTypeKey;
            Cardinality = cardinality;
            Role = role;
            ConditionKey = conditionKey;
            BindingKey = bindingKey;
            SupportsCompatibleStableTarget = supportsCompatibleStableTarget;
        }
    }

    public sealed class D3MTypeRegistration
    {
        private readonly Dictionary<string, D3MRegisteredRelationship> _relationships =
            new Dictionary<string, D3MRegisteredRelationship>(StringComparer.Ordinal);

        public string TypeKey { get; }
        public D3MStorageKind StorageKind { get; }
        public string ApplicationDataCollection { get; }
        public bool AllowTopLevelSelection { get; }
        public IReadOnlyCollection<D3MRegisteredRelationship> Relationships => _relationships.Values;

        public D3MTypeRegistration(string typeKey, D3MStorageKind storageKind,
            bool allowTopLevelSelection = false, string applicationDataCollection = null)
        {
            if (String.IsNullOrWhiteSpace(typeKey)) throw new ArgumentException("Type key is required.", nameof(typeKey));
            if (!Enum.IsDefined(typeof(D3MStorageKind), storageKind))
                throw new ArgumentOutOfRangeException(nameof(storageKind));
            if (storageKind == D3MStorageKind.ApplicationData && String.IsNullOrWhiteSpace(applicationDataCollection))
                throw new ArgumentException("ApplicationData must have its registered collection.", nameof(applicationDataCollection));
            if (storageKind != D3MStorageKind.ApplicationData && !String.IsNullOrWhiteSpace(applicationDataCollection))
                throw new ArgumentException("Only ApplicationData has an ApplicationData collection.", nameof(applicationDataCollection));
            TypeKey = typeKey.Trim();
            StorageKind = storageKind;
            ApplicationDataCollection = applicationDataCollection;
            AllowTopLevelSelection = allowTopLevelSelection;
        }

        /// <summary>Declare a real, code-resolved immediate relationship, not a reflected path.</summary>
        public D3MTypeRegistration AddRelationship(string propertyPath, string targetTypeKey,
            D3MCardinality cardinality, D3MRelationshipRole role = D3MRelationshipRole.Prerequisite,
            string conditionKey = null, string bindingKey = null, bool supportsCompatibleStableTarget = false)
        {
            if (String.IsNullOrWhiteSpace(propertyPath)) throw new ArgumentException("Property path required.", nameof(propertyPath));
            if (String.IsNullOrWhiteSpace(targetTypeKey)) throw new ArgumentException("Target type required.", nameof(targetTypeKey));
            if (!Enum.IsDefined(typeof(D3MCardinality), cardinality)) throw new ArgumentOutOfRangeException(nameof(cardinality));
            if (!Enum.IsDefined(typeof(D3MRelationshipRole), role)) throw new ArgumentOutOfRangeException(nameof(role));
            var path = propertyPath.Trim();
            if (_relationships.ContainsKey(path))
                throw new InvalidOperationException("A registered type cannot declare the same path twice: " + path);
            _relationships.Add(path, new D3MRegisteredRelationship(path, targetTypeKey.Trim(), cardinality,
                role, conditionKey, bindingKey, supportsCompatibleStableTarget));
            return this;
        }

        public bool TryGetRelationship(string propertyPath, out D3MRegisteredRelationship relationship)
            => _relationships.TryGetValue(propertyPath ?? String.Empty, out relationship);
    }

    public sealed class D3MTypeRegistry
    {
        private readonly Dictionary<string, D3MTypeRegistration> _types =
            new Dictionary<string, D3MTypeRegistration>(StringComparer.Ordinal);

        public D3MTypeRegistry Register(D3MTypeRegistration registration)
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));
            if (_types.ContainsKey(registration.TypeKey))
                throw new InvalidOperationException("Duplicate D3M type registration: " + registration.TypeKey);
            _types.Add(registration.TypeKey, registration);
            return this;
        }

        public bool TryGet(string typeKey, out D3MTypeRegistration registration)
            => _types.TryGetValue(typeKey ?? String.Empty, out registration);

        public bool Contains(string typeKey) => _types.ContainsKey(typeKey ?? String.Empty);
    }

    public sealed class D3MValidationIssue
    {
        public D3MValidationIssue(string code, string path, string message)
        {
            Code = code;
            Path = path;
            Message = message;
        }

        public string Code { get; }
        public string Path { get; }
        public string Message { get; }
    }

    public sealed class D3MValidationResult
    {
        internal D3MValidationResult(List<D3MValidationIssue> issues) { Issues = issues.AsReadOnly(); }
        public IReadOnlyList<D3MValidationIssue> Issues { get; }
        public bool IsValid => Issues.Count == 0;
    }

    /// <summary>Pure, deterministic publication validation against a registered type universe.</summary>
    public sealed class D3MValidator
    {
        private readonly D3MTypeRegistry _registry;

        public D3MValidator(D3MTypeRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public D3MValidationResult Validate(D3MDefinitionRecord definition)
        {
            var issues = new List<D3MValidationIssue>();
            void Add(string code, string path, string message) =>
                issues.Add(new D3MValidationIssue(code, path, message));

            if (definition == null)
            {
                Add("missing-definition", "definition", "Definition is required.");
                return new D3MValidationResult(issues);
            }

            if (definition.Organization == null || String.IsNullOrWhiteSpace(definition.Organization.Id))
                Add("missing-organization", "organization", "Authoritative organization scope is required.");
            if (definition.SchemaVersion != 1)
                Add("unsupported-schema", "schemaVersion", "Only schema version 1 is supported.");
            if (definition.DraftRevision < 1)
                Add("invalid-revision", "draftRevision", "Draft revision must be positive.");
            if (String.IsNullOrWhiteSpace(definition.Name))
                Add("missing-name", "name", "Friendly name is required.");
            if (definition.Retired || !definition.Enabled)
                Add("inactive-definition", "enabled", "Disabled or retired definitions cannot be published.");
            if (!Enum.IsDefined(typeof(D3MStorageKind), definition.StorageKind))
                Add("invalid-storage-kind", "storageKind", "Unknown transfer classification.");

            if (!_registry.TryGet(definition.TypeKey, out var source))
            {
                Add("unknown-type", "typeKey", "The source record type has not been registered.");
                return new D3MValidationResult(issues);
            }

            if (definition.Id.Value != D3MIdentity.Definition(source.TypeKey).Value)
                Add("invalid-identity", "id", "Definition Id must be the deterministic registered type key.");
            if (definition.StorageKind != source.StorageKind)
                Add("storage-mismatch", "storageKind", "Transfer classification must match the registered handler.");
            if (!String.Equals(definition.ApplicationDataCollection, source.ApplicationDataCollection, StringComparison.Ordinal))
                Add("collection-mismatch", "applicationDataCollection", "Collection must match the registered ApplicationData collection.");
            if (definition.TopLevelSelectable && !source.AllowTopLevelSelection)
                Add("root-not-allowed", "topLevelSelectable", "This type is not approved for top-level selection.");

            var rules = definition.Dependencies ?? new List<D3MDependencyRule>();
            if (definition.ExplicitLeaf && rules.Count != 0)
                Add("leaf-has-dependencies", "dependencies", "An explicit leaf may not have dependencies.");
            if (!definition.ExplicitLeaf && rules.Count == 0)
                Add("missing-explicit-leaf", "explicitLeaf", "Declare immediate dependencies or explicitly identify the type as a leaf.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                var path = "dependencies[" + i + "]";
                if (rule == null)
                {
                    Add("null-rule", path, "A dependency rule may not be null.");
                    continue;
                }
                if (String.IsNullOrWhiteSpace(rule.PropertyPath) || !seen.Add(rule.PropertyPath))
                {
                    Add("missing-or-duplicate-path", path + ".propertyPath", "Each rule needs one distinct registered path.");
                    continue;
                }
                if (!source.TryGetRelationship(rule.PropertyPath, out var registered))
                {
                    Add("unknown-path", path + ".propertyPath", "Dependency path has no registered domain accessor.");
                    continue;
                }
                if (!_registry.Contains(rule.TargetTypeKey))
                    Add("unknown-target", path + ".targetTypeKey", "Target type has no registered handler.");
                if (!String.Equals(rule.TargetTypeKey, registered.TargetTypeKey, StringComparison.Ordinal))
                    Add("target-mismatch", path + ".targetTypeKey", "Target type does not match registered relationship.");
                if (!Enum.IsDefined(typeof(D3MCardinality), rule.Cardinality) || rule.Cardinality != registered.Cardinality)
                    Add("cardinality-mismatch", path + ".cardinality", "Cardinality does not match registered accessor.");
                if (!Enum.IsDefined(typeof(D3MPresence), rule.Presence))
                    Add("invalid-presence", path + ".presence", "Unknown required/optional/conditional policy.");
                if (!Enum.IsDefined(typeof(D3MRelationshipRole), rule.Role) || rule.Role != registered.Role)
                    Add("role-mismatch", path + ".role", "Prerequisite/navigation role does not match registered accessor.");
                if (!Enum.IsDefined(typeof(D3MVersionRequirement), rule.VersionRequirement) ||
                    (rule.VersionRequirement == D3MVersionRequirement.CompatibleStableTarget && !registered.SupportsCompatibleStableTarget))
                    Add("unsupported-compatibility", path + ".versionRequirement", "No registered handler can prove target-version compatibility.");
                if (rule.Presence == D3MPresence.Conditional)
                {
                    if (String.IsNullOrWhiteSpace(registered.ConditionKey) ||
                        !String.Equals(rule.ConditionKey, registered.ConditionKey, StringComparison.Ordinal))
                        Add("unregistered-condition", path + ".conditionKey", "Conditional participation requires its registered predicate.");
                }
                else if (!String.IsNullOrWhiteSpace(rule.ConditionKey))
                    Add("unexpected-condition", path + ".conditionKey", "Only conditional rules may name a predicate.");
                if (!String.Equals(rule.BindingKey, registered.BindingKey, StringComparison.Ordinal))
                    Add("binding-mismatch", path + ".bindingKey", "Target-local binding must match the registered handler.");
            }

            return new D3MValidationResult(issues);
        }
    }
}
