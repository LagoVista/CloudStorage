using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public enum StorageRecordClass
    {
        ApplicationData,
        ActivityRecord,
        Scratch,
        Metrics
    }

    public sealed class StorageRetentionRule
    {
        public StorageRecordClass RecordClass { get; set; }
        public string Scope { get; set; }
        public TimeSpan? Ttl { get; set; }
        public bool Protected { get; set; }
        public bool SummarizeBeforeExpiry { get; set; }

        public void Validate()
        {
            if (Ttl.HasValue && Ttl.Value <= TimeSpan.Zero)
                throw new InvalidOperationException("Retention TTL must be greater than zero.");
            if (Protected && Ttl.HasValue)
                throw new InvalidOperationException("Protected retention rules cannot also define a TTL.");
        }
    }

    public sealed class StorageRetentionDecision
    {
        internal StorageRetentionDecision(TimeSpan? effectiveTtl, bool isProtected, bool summarizeBeforeExpiry, bool explicitlyGoverned, string source)
        {
            EffectiveTtl = effectiveTtl;
            IsProtected = isProtected;
            SummarizeBeforeExpiry = summarizeBeforeExpiry;
            ExplicitlyGoverned = explicitlyGoverned;
            Source = source;
        }

        public TimeSpan? EffectiveTtl { get; }
        public bool IsProtected { get; }
        public bool SummarizeBeforeExpiry { get; }
        public bool ExplicitlyGoverned { get; }
        public string Source { get; }

        public static StorageRetentionDecision DurableDefault(string source = "unconfigured")
            => new StorageRetentionDecision(null, false, false, false, source);
    }

    public sealed class StorageRetentionPolicy
    {
        private readonly List<StorageRetentionRule> _rules = new List<StorageRetentionRule>();
        public IReadOnlyList<StorageRetentionRule> Rules => _rules;

        public StorageRetentionPolicy AddDefault(StorageRecordClass recordClass, TimeSpan? ttl = null, bool isProtected = false, bool summarizeBeforeExpiry = false)
        {
            return AddRule(recordClass, null, ttl, isProtected, summarizeBeforeExpiry);
        }

        public StorageRetentionPolicy AddOverride(StorageRecordClass recordClass, string scope, TimeSpan? ttl = null, bool isProtected = false, bool summarizeBeforeExpiry = false)
        {
            if (String.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Retention override scope is required.", nameof(scope));
            return AddRule(recordClass, scope.Trim(), ttl, isProtected, summarizeBeforeExpiry);
        }

        public StorageRetentionDecision Resolve(StorageRecordClass recordClass, string scope = null)
        {
            var scoped = String.IsNullOrWhiteSpace(scope)
                ? null
                : _rules.LastOrDefault(rule => rule.RecordClass == recordClass && String.Equals(rule.Scope, scope.Trim(), StringComparison.OrdinalIgnoreCase));
            var rule = scoped ?? _rules.LastOrDefault(item => item.RecordClass == recordClass && String.IsNullOrWhiteSpace(item.Scope));
            if (rule == null)
                return StorageRetentionDecision.DurableDefault();

            rule.Validate();
            return new StorageRetentionDecision(
                rule.Protected ? null : rule.Ttl,
                rule.Protected,
                rule.SummarizeBeforeExpiry,
                true,
                scoped == null ? "record-class-default" : "scoped-override");
        }

        public bool CanExpire(StorageRecordClass recordClass)
        {
            return _rules.Any(rule => rule.RecordClass == recordClass && !rule.Protected && rule.Ttl.HasValue);
        }

        private StorageRetentionPolicy AddRule(StorageRecordClass recordClass, string scope, TimeSpan? ttl, bool isProtected, bool summarizeBeforeExpiry)
        {
            var rule = new StorageRetentionRule
            {
                RecordClass = recordClass,
                Scope = scope,
                Ttl = ttl,
                Protected = isProtected,
                SummarizeBeforeExpiry = summarizeBeforeExpiry
            };
            rule.Validate();
            _rules.Add(rule);
            return this;
        }
    }

    public sealed class StorageRetentionPolicyRecord : IApplicationDataRecord
    {
        public NormalizedId32 Id { get; set; }
        public EntityHeader Organization { get; set; }
        public UtcTimestamp CreationDate { get; set; }
        public UtcTimestamp LastUpdatedDate { get; set; }
        public List<StorageRetentionRule> Rules { get; set; } = new List<StorageRetentionRule>();

        public StorageRetentionPolicy ToPolicy()
        {
            var policy = new StorageRetentionPolicy();
            foreach (var rule in Rules ?? new List<StorageRetentionRule>())
            {
                if (String.IsNullOrWhiteSpace(rule.Scope))
                    policy.AddDefault(rule.RecordClass, rule.Ttl, rule.Protected, rule.SummarizeBeforeExpiry);
                else
                    policy.AddOverride(rule.RecordClass, rule.Scope, rule.Ttl, rule.Protected, rule.SummarizeBeforeExpiry);
            }

            return policy;
        }
    }

    public interface IStorageRetentionPolicyStore
    {
        Task<VersionedApplicationDataRecord<StorageRetentionPolicyRecord>> GetVersionedAsync(StorageKey key, CancellationToken cancellationToken = default);
        Task InsertAsync(StorageRetentionPolicyRecord record, CancellationToken cancellationToken = default);
        Task<ApplicationDataMutationResult> UpdateIfVersionAsync(StorageRetentionPolicyRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default);
    }

    public sealed class StorageRetentionPolicyStore : IStorageRetentionPolicyStore
    {
        private readonly IApplicationDataStore _applicationData;

        public StorageRetentionPolicyStore(IApplicationDataStore applicationData)
        {
            _applicationData = applicationData ?? throw new ArgumentNullException(nameof(applicationData));
        }

        public Task<VersionedApplicationDataRecord<StorageRetentionPolicyRecord>> GetVersionedAsync(StorageKey key, CancellationToken cancellationToken = default)
            => _applicationData.GetVersionedAsync<StorageRetentionPolicyRecord>(key, cancellationToken);

        public Task InsertAsync(StorageRetentionPolicyRecord record, CancellationToken cancellationToken = default)
            => _applicationData.InsertAsync(record, cancellationToken);

        public Task<ApplicationDataMutationResult> UpdateIfVersionAsync(StorageRetentionPolicyRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default)
            => _applicationData.UpdateIfVersionAsync(record, expectedVersion, cancellationToken);
    }
}
