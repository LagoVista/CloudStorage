using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>Only registered, domain-owned handlers may implement these five transfer classes.</summary>
    public enum D3MStorageKind { EntityBase, ApplicationData, Product, ProductCategory, Media }
    public enum D3MCardinality { One, Many }
    public enum D3MPresence { Required, Optional, Conditional }
    public enum D3MRelationshipRole { Prerequisite, NavigationOnly }
    public enum D3MVersionRequirement { ExactApprovedSource, CompatibleStableTarget }

    /// <summary>One immediate dependency; never a transitive dependency specification.</summary>
    public sealed class D3MDependencyRule
    {
        public string PropertyPath { get; set; }
        public string TargetTypeKey { get; set; }
        public D3MCardinality Cardinality { get; set; }
        public D3MPresence Presence { get; set; }
        public D3MRelationshipRole Role { get; set; } = D3MRelationshipRole.Prerequisite;
        public D3MVersionRequirement VersionRequirement { get; set; } = D3MVersionRequirement.ExactApprovedSource;
        /// <summary>Pre-registered, deterministic predicate; never executable source or a query.</summary>
        public string ConditionKey { get; set; }
        /// <summary>Pre-registered target-local resolution handler; never a secret or connection string.</summary>
        public string BindingKey { get; set; }
    }

    /// <summary>
    /// Mutable authoring record: one organization-scoped record per registered domain type.
    /// A published map is NEVER read from this mutable draft for promotion.
    /// </summary>
    public sealed class D3MDefinitionRecord : IApplicationDataRecord
    {
        public NormalizedId32 Id { get; set; }
        public EntityHeader Organization { get; set; }
        public UtcTimestamp CreationDate { get; set; }
        public UtcTimestamp LastUpdatedDate { get; set; }
        public string TypeKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public D3MStorageKind StorageKind { get; set; }
        public string ApplicationDataCollection { get; set; }
        public int SchemaVersion { get; set; } = 1;
        public int DraftRevision { get; set; } = 1;
        public int? PublishedRevision { get; set; }
        public string PublishedBy { get; set; }
        public DateTime? PublishedUtc { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Retired { get; set; }
        public bool TopLevelSelectable { get; set; }
        public bool ExplicitLeaf { get; set; }
        public List<D3MDependencyRule> Dependencies { get; set; } = new List<D3MDependencyRule>();
    }

    /// <summary>
    /// An immutable, version-pinned publication. This distinct ApplicationData type has
    /// its own physical collection; write it only by insert, never by replacement.
    /// </summary>
    public sealed class D3MPublishedDefinitionRecord : IApplicationDataRecord
    {
        public NormalizedId32 Id { get; set; }
        public EntityHeader Organization { get; set; }
        public UtcTimestamp CreationDate { get; set; }
        public UtcTimestamp LastUpdatedDate { get; set; }
        public string TypeKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public D3MStorageKind StorageKind { get; set; }
        public string ApplicationDataCollection { get; set; }
        public int SchemaVersion { get; set; }
        public int Revision { get; set; }
        public string PublishedBy { get; set; }
        public DateTime PublishedUtc { get; set; }
        public bool TopLevelSelectable { get; set; }
        public bool ExplicitLeaf { get; set; }
        public List<D3MDependencyRule> Dependencies { get; set; } = new List<D3MDependencyRule>();
    }

    /// <summary>Deterministic type+revision IDs, with Organization.Id supplying the distinct scope.</summary>
    public static class D3MIdentity
    {
        public static NormalizedId32 Definition(string typeKey) => Hash("d3m:definition:" + RequiredKey(typeKey));
        public static NormalizedId32 Published(string typeKey, int revision)
        {
            if (revision < 1) throw new ArgumentOutOfRangeException(nameof(revision));
            return Hash("d3m:published:" + RequiredKey(typeKey) + ":" +
                revision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static string RequiredKey(string key)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentException("Registered type key is required.", nameof(key));
            return key.Trim().ToLowerInvariant();
        }

        private static NormalizedId32 Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return new NormalizedId32(BitConverter.ToString(bytes, 0, 16).Replace("-", "").ToLowerInvariant());
            }
        }
    }
}
