using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class CacheProvider : ICacheProvider
    {
        private const string DependentsSuffix = ":dependents";

        private readonly IAdminLogger _logger;
        private readonly ConnectionMultiplexer _multiplexer = null;

        private static readonly Dictionary<string, string> _inMemoryCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); 
        private static readonly Dictionary<string, Dictionary<string, string>> _inMemoryCollections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public CacheProvider(ICacheProviderSettings settings, IAdminLogger adminlogger, IAppConfig appConfig)
        {
            _logger = adminlogger ?? throw new ArgumentNullException(nameof(adminlogger));

            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings.UseCache)
            {
                var uri = settings.CacheSettings.Uri;

                try
                {
                    var configuration = ConfigurationOptions.Parse(uri);
                    if (settings.UseAuthentication)
                    {
                        if (String.IsNullOrWhiteSpace(settings.Password))
                        {
                            throw new InvalidOperationException("Cache authentication is enabled but no password was configured.");
                        }

                        configuration.Password = settings.Password;
                    }

                    _logger.Trace($"{this.Tag()} - Initializing cache provider with settings: {uri} - Slot Title {appConfig.Environment}, {appConfig.AppId} ");
                    _multiplexer = ConnectionMultiplexer.Connect(configuration);
                    _logger.Trace($"{this.Tag()} - Established connection to REDIS-compatible cache: {uri}");
                }
                catch (Exception ex)
                {
                    _logger.AddError(this.Tag(), $"Failed to connect to REDIS-compatible cache with settings: {uri}. Exception: {ex}. You can disable remote cache by setting UseCache = false in AppSettings.");
                    throw;
                }
            }
        }

        public static readonly TimeSpan DefaultTTL = TimeSpan.FromMinutes(30);

        public async Task<bool> AttemptAcquireLockAsync(string key, string token, TimeSpan? expires = null)
        {
            if (_multiplexer != null)
            {
                var normalizedKey = NormalizeKey(key);
                var db = _multiplexer.GetDatabase();
                return await db.LockTakeAsync(normalizedKey, token, expires ?? TimeSpan.FromSeconds(30));
            }

            return false;
        }

        public async Task<bool> ExtendLockAsync(string key, string token, TimeSpan? expires = null)
        {
            if (_multiplexer != null)
            {

                var normalizedKey = NormalizeKey(key);
                var db = _multiplexer.GetDatabase();
                return await db.LockExtendAsync(normalizedKey, token, expires ?? TimeSpan.FromSeconds(30));
            }

            return false;
        }

        public async Task<bool> ReleaseLockAsync(string key, string toke)
        {
            if (_multiplexer != null)
            {
                var normalizedKey = NormalizeKey(key);

                var db = _multiplexer.GetDatabase();
                return await db.LockReleaseAsync(normalizedKey, toke);
            }

            return false;
        }

        public Task AddAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return AddAsync(key, JsonConvert.SerializeObject(value), ttl ?? DefaultTTL);
        }

        public async Task AddAsync<T>(string key, T value, IEnumerable<string> dependencyKeys, TimeSpan? ttl = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            await AddAsync(key, JsonConvert.SerializeObject(value), dependencyKeys, ttl ?? DefaultTTL);
        }

        public async Task AddAsync(string key, string value, TimeSpan? ttl = null)
        {
            ValidateKey(key);

            await AddCacheValueAsync(key, value, ttl ?? DefaultTTL);
            await InvalidateDependentsAsync(key);
        }

        public async Task AddAsync(string key, string value, IEnumerable<string> dependencyKeys, TimeSpan? ttl = null)
        {
            await AddAsync(key, value, ttl);

            if (dependencyKeys == null)
            {
                return;
            }

            foreach (var dependencyKey in dependencyKeys.Where(dependencyKey => !String.IsNullOrWhiteSpace(dependencyKey)))
            {
                await AddDependentAsync(dependencyKey, key, ttl ?? DefaultTTL);
            }
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var value = await GetCacheValueAsync(key);
            if (String.IsNullOrWhiteSpace(value))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(value);
        }

        public async Task<string> GetAsync(string key)
        {
            return await GetCacheValueAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            ValidateKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                await db.KeyDeleteAsync(NormalizeKey(key));
            }
            else
            {
                _inMemoryCache.Remove(NormalizeKey(key));
            }

            await InvalidateDependentsAsync(key);
        }

        public async Task AddToCollectionAsync<T>(string collectionKey, string itemKey, T value, TimeSpan? ttl = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            await AddToCollectionAsync(collectionKey, itemKey, JsonConvert.SerializeObject(value), ttl);
        }

        public async Task AddToCollectionAsync(string collectionKey, string itemKey, string value, TimeSpan? ttl = null)
        {
            ValidateKey(collectionKey);
            ValidateKey(itemKey);

            var normalizedCollectionKey = NormalizeKey(collectionKey);
            var normalizedItemKey = NormalizeKey(itemKey);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                await db.HashSetAsync(normalizedCollectionKey, normalizedItemKey, value);
                await db.KeyExpireAsync(normalizedCollectionKey, ttl ?? DefaultTTL);
            }
            else
            {
                if (!_inMemoryCollections.TryGetValue(normalizedCollectionKey, out var collection))
                {
                    collection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _inMemoryCollections[normalizedCollectionKey] = collection;
                }

                collection[normalizedItemKey] = value;
            }
        }

        public async Task<T> GetFromCollectionAsync<T>(string collectionKey, string itemKey)
        {
            var value = await GetFromCollectionAsync(collectionKey, itemKey);
            if (String.IsNullOrWhiteSpace(value))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(value);
        }

        public async Task<string> GetFromCollectionAsync(string collectionKey, string itemKey)
        {
            ValidateKey(collectionKey);
            ValidateKey(itemKey);

            var normalizedCollectionKey = NormalizeKey(collectionKey);
            var normalizedItemKey = NormalizeKey(itemKey);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                return await db.HashGetAsync(normalizedCollectionKey, normalizedItemKey);
            }

            if (_inMemoryCollections.TryGetValue(normalizedCollectionKey, out var collection) && collection.TryGetValue(normalizedItemKey, out var value))
            {
                return value;
            }

            return null;
        }

        public async Task RemoveFromCollectionAsync(string collectionKey, string itemKey)
        {
            ValidateKey(collectionKey);
            ValidateKey(itemKey);

            var normalizedCollectionKey = NormalizeKey(collectionKey);
            var normalizedItemKey = NormalizeKey(itemKey);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                await db.HashDeleteAsync(normalizedCollectionKey, normalizedItemKey);
            }
            else if (_inMemoryCollections.TryGetValue(normalizedCollectionKey, out var collection))
            {
                collection.Remove(normalizedItemKey);
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            ValidateKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                return await db.StringIncrementAsync(NormalizeKey(key), value);
            }

            var normalizedKey = NormalizeKey(key);
            if (!_inMemoryCache.TryGetValue(normalizedKey, out var existing) || !Int64.TryParse(existing, NumberStyles.Integer, CultureInfo.InvariantCulture, out var current))
            {
                current = 0;
            }

            current += value;
            _inMemoryCache[normalizedKey] = current.ToString(CultureInfo.InvariantCulture);
            return current;
        }

        private async Task AddDependentAsync(string dependencyKey, string dependentKey, TimeSpan ttl)
        {
            var normalizedDependencyKey = NormalizeKey(dependencyKey) + DependentsSuffix;
            var normalizedDependentKey = NormalizeKey(dependentKey);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                await db.SetAddAsync(normalizedDependencyKey, normalizedDependentKey);
                await db.KeyExpireAsync(normalizedDependencyKey, ttl);
            }
            else
            {
                if (!_inMemoryCollections.TryGetValue(normalizedDependencyKey, out var collection))
                {
                    collection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _inMemoryCollections[normalizedDependencyKey] = collection;
                }

                collection[normalizedDependentKey] = normalizedDependentKey;
            }
        }

        private async Task InvalidateDependentsAsync(string dependencyKey)
        {
            var normalizedDependencyKey = NormalizeKey(dependencyKey) + DependentsSuffix;

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                var dependents = await db.SetMembersAsync(normalizedDependencyKey);
                if (dependents.Length > 0)
                {
                    await db.KeyDeleteAsync(dependents.Select(dependent => (RedisKey)dependent.ToString()).ToArray());
                }

                await db.KeyDeleteAsync(normalizedDependencyKey);
            }
            else if (_inMemoryCollections.TryGetValue(normalizedDependencyKey, out var collection))
            {
                foreach (var dependentKey in collection.Keys.ToList())
                {
                    _inMemoryCache.Remove(dependentKey);
                    _inMemoryCollections.Remove(dependentKey);
                }

                _inMemoryCollections.Remove(normalizedDependencyKey);
            }
        }

        private async Task AddCacheValueAsync(string key, string value, TimeSpan ttl)
        {
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                await db.StringSetAsync(normalizedKey, value, ttl);
            }
            else
            {
                _inMemoryCache[normalizedKey] = value;
            }
        }

        private async Task<string> GetCacheValueAsync(string key)
        {
            ValidateKey(key);

            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                return await db.StringGetAsync(normalizedKey);
            }

            _inMemoryCache.TryGetValue(normalizedKey, out var value);
            return value;
        }

        private static string NormalizeKey(string key)
        {
            return key.Trim().ToLowerInvariant();
        }

        private static void ValidateKey(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
        }
    }
}
