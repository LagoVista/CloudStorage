using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.RuntimeControls
{
    /// <summary>Known operational controls. Only registered keys may be read or changed.</summary>
    public sealed class RuntimeControlDefinition
    {
        public string Key { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public bool IsBoolean { get; set; } = true;
    }

    public sealed class RuntimeControlState
    {
        public string Key { get; set; }
        public string EffectiveValue { get; set; }
        public bool HasOverride { get; set; }
        public long? TtlSeconds { get; set; }
    }

    public static class RuntimeControlCatalog
    {
        public const string Diagnostics = "CloudStorage.Diagnostics.Enabled";
        public const string MongoQueries = "CloudStorage.Diagnostics.LogQueries";
        public const string CacheProviderDiagnostics = "CloudStorage.Diagnostics.CacheProvider";
        public const string SlowQueryThreshold = "CloudStorage.Diagnostics.SlowQueryThresholdMs";
        public const string LoggingVerbose = "Logging.Diagnostics.Verbose";
        public const string Prefix = "nuviot:runtime-controls:v1:";

        public static readonly IReadOnlyList<RuntimeControlDefinition> Definitions =
            new List<RuntimeControlDefinition>
            {
                new RuntimeControlDefinition { Key = Diagnostics, Group = "Cloud Storage", Description = "Log routine document operations.", DefaultValue = "false" },
                new RuntimeControlDefinition { Key = MongoQueries, Group = "Cloud Storage", Description = "Log MongoDB query details.", DefaultValue = "false" },
                new RuntimeControlDefinition { Key = CacheProviderDiagnostics, Group = "Cloud Storage", Description = "Log routine CacheProvider reads, writes, hits, misses and invalidations.", DefaultValue = "false" },
                new RuntimeControlDefinition { Key = SlowQueryThreshold, Group = "Cloud Storage", Description = "Slow query threshold in milliseconds.", DefaultValue = "500", IsBoolean = false },
                new RuntimeControlDefinition { Key = LoggingVerbose, Group = "Logging", Description = "Persist verbose trace, debug, and JSON diagnostic events to Cassandra.", DefaultValue = "false" }
            }.AsReadOnly();

        public static RuntimeControlDefinition Find(string key)
        {
            var result = Definitions.FirstOrDefault(item => String.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
            if (result == null) throw new ArgumentException($"Unknown runtime control '{key}'.", nameof(key));
            return result;
        }

        public static string StorageKey(string key) => Prefix + Find(key).Key.ToLowerInvariant();

        public static string NormalizeValue(RuntimeControlDefinition control, string value)
        {
            if (control.IsBoolean && Boolean.TryParse(value, out var enabled)) return enabled ? "true" : "false";
            if (!control.IsBoolean && Int32.TryParse(value, out var number) && number >= 0) return number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            throw new ArgumentException($"Invalid value for runtime control '{control.Key}'.", nameof(value));
        }
    }

    public interface IRuntimeControlService
    {
        bool IsEnabled(string key);
        int GetInt32(string key);
        Task<RuntimeControlState> GetAsync(string key, CancellationToken cancellationToken = default);
        Task<RuntimeControlState> SetAsync(string key, string value, long? ttlSeconds = null, CancellationToken cancellationToken = default);
        Task ClearAsync(string key, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Valkey is authoritative for active overrides. Local snapshots make hot-path checks synchronous.
    /// Expired overrides always resolve to the catalog default, even before the next refresh.
    /// </summary>
    public sealed class RuntimeControlService : IRuntimeControlService
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(5);
        private readonly IValkeyAdminRepo _valkey;
        private readonly ConcurrentDictionary<string, Snapshot> _snapshots =
            new ConcurrentDictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _refreshing =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        private sealed class Snapshot
        {
            public string Value { get; set; }
            public DateTimeOffset RefreshAfterUtc { get; set; }
            public DateTimeOffset? ExpiresAtUtc { get; set; }
        }

        public RuntimeControlService(IValkeyAdminRepo valkey)
        {
            _valkey = valkey ?? throw new ArgumentNullException(nameof(valkey));
        }

        public bool IsEnabled(string key)
        {
            var definition = RuntimeControlCatalog.Find(key);
            if (!definition.IsBoolean) throw new ArgumentException("Control is not boolean.", nameof(key));
            return Boolean.Parse(ReadLocal(definition));
        }

        public int GetInt32(string key)
        {
            var definition = RuntimeControlCatalog.Find(key);
            if (definition.IsBoolean) throw new ArgumentException("Control is not numeric.", nameof(key));
            return Int32.Parse(ReadLocal(definition), System.Globalization.CultureInfo.InvariantCulture);
        }

        private string ReadLocal(RuntimeControlDefinition definition)
        {
            var now = DateTimeOffset.UtcNow;
            if (!_snapshots.TryGetValue(definition.Key, out var snapshot))
            {
                StartRefresh(definition.Key);
                return definition.DefaultValue;
            }

            if (now >= snapshot.RefreshAfterUtc) StartRefresh(definition.Key);
            if (snapshot.ExpiresAtUtc.HasValue && now >= snapshot.ExpiresAtUtc.Value)
                return definition.DefaultValue;

            return snapshot.Value;
        }

        private void StartRefresh(string key)
        {
            if (!_refreshing.TryAdd(key, 0)) return;
            _ = RefreshAsync(key);
        }

        private async Task RefreshAsync(string key)
        {
            try
            {
                await GetAsync(key).ConfigureAwait(false);
            }
            catch
            {
                // The hot logging path must not fail when Valkey is unavailable.
                // Revert to the default, and retry on the next check.
                var definition = RuntimeControlCatalog.Find(key);
                _snapshots[key] = new Snapshot
                {
                    Value = definition.DefaultValue,
                    RefreshAfterUtc = DateTimeOffset.UtcNow.Add(RefreshInterval)
                };
            }
            finally
            {
                _refreshing.TryRemove(key, out _);
            }
        }

        public async Task<RuntimeControlState> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var definition = RuntimeControlCatalog.Find(key);
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _valkey.GetAsync(0, RuntimeControlCatalog.StorageKey(key), cancellationToken).ConfigureAwait(false);
            var hasOverride = !String.IsNullOrWhiteSpace(result?.Value);
            var value = hasOverride ? RuntimeControlCatalog.NormalizeValue(definition, result.Value) : definition.DefaultValue;
            var now = DateTimeOffset.UtcNow;
            var ttl = hasOverride ? result.TtlSeconds : null;
            _snapshots[definition.Key] = new Snapshot
            {
                Value = value,
                RefreshAfterUtc = now.Add(RefreshInterval),
                ExpiresAtUtc = ttl.HasValue ? now.AddSeconds(ttl.Value) : (DateTimeOffset?)null
            };
            return new RuntimeControlState { Key = definition.Key, EffectiveValue = value, HasOverride = hasOverride, TtlSeconds = ttl };
        }

        public async Task<RuntimeControlState> SetAsync(string key, string value, long? ttlSeconds = null, CancellationToken cancellationToken = default)
        {
            var definition = RuntimeControlCatalog.Find(key);
            if (ttlSeconds.HasValue && (ttlSeconds.Value <= 0 || ttlSeconds.Value > 60L * 60 * 24 * 365))
                throw new ArgumentOutOfRangeException(nameof(ttlSeconds), "TTL must be between one second and one year.");
            var normalized = RuntimeControlCatalog.NormalizeValue(definition, value);
            cancellationToken.ThrowIfCancellationRequested();
            await _valkey.SetAsync(new ValkeySetRequest
            {
                Database = 0, Key = RuntimeControlCatalog.StorageKey(key), Value = normalized, TtlSeconds = ttlSeconds
            }, cancellationToken).ConfigureAwait(false);
            return await GetAsync(key, cancellationToken).ConfigureAwait(false);
        }

        public async Task ClearAsync(string key, CancellationToken cancellationToken = default)
        {
            var definition = RuntimeControlCatalog.Find(key);
            cancellationToken.ThrowIfCancellationRequested();
            await _valkey.DeleteAsync(0, RuntimeControlCatalog.StorageKey(key), cancellationToken).ConfigureAwait(false);
            _snapshots.TryRemove(definition.Key, out _);
        }
    }
}
