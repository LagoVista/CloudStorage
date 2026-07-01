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
                    _logger.Trace($"{this.Tag()} - Initializing cache provider with settings: {uri} - Slot Title {appConfig.Environment}, {appConfig.AppId} ");
                    _multiplexer = ConnectionMultiplexer.Connect(uri);
                    _logger.Trace($"{this.Tag()} - Established connection to REDIS: {uri}");
                }
                catch (Exception ex)
                {
                    _logger.AddError(this.Tag(), $"Failed to connect to REDIS with settings: {uri}. Exception: {ex}.  You can disable remote cache by setting UseCache = false in AppSettings.  Or you can setup a SSH tunnel to dev cache server.");
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
            await RegisterDependenciesAsync(key, dependencyKeys);
        }

        public async Task RegisterDependenciesAsync(string key, IEnumerable<string> dependencyKeys)
        {
            if (dependencyKeys == null)
            {
                return;
            }

            var normalizedKey = NormalizeKey(key);
            var normalizedDependencyKeys = dependencyKeys
                .Where(dependencyKey => !string.IsNullOrWhiteSpace(dependencyKey))
                .Select(NormalizeKey)
                .Where(dependencyKey => !String.Equals(dependencyKey, normalizedKey, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var dependencyKey in normalizedDependencyKeys)
            {
                var dependentsCollectionKey = GetDependentsCollectionKey(dependencyKey);
                await AddToCollectionAsync(dependentsCollectionKey, normalizedKey, normalizedKey);
            }
        }

        public Task AddToCollectionAsync(string collectionKey, string key, string value)
        {
            var normalizedCollectionKey = NormalizeKey(collectionKey);
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                return db.HashSetAsync(normalizedCollectionKey, normalizedKey, value);
            }

            lock (_inMemoryCollections)
            {
                if (!_inMemoryCollections.TryGetValue(normalizedCollectionKey, out var collection))
                {
                    collection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _inMemoryCollections[normalizedCollectionKey] = collection;
                }

                collection[normalizedKey] = value;
            }

            return Task.CompletedTask;
        }

        public async Task<string> GetAsync(string key)
        {
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                var result = await db.StringGetAsync(normalizedKey);
                var str = (string)result;

                _logger.Trace($"{this.Tag()} - Getting cache item: {normalizedKey} found in cache: {!String.IsNullOrEmpty(str)}");
                return str;
            }

            lock (_inMemoryCache)
            {
                if (_inMemoryCache.TryGetValue(normalizedKey, out var value))
                {
                    Console.WriteLine($"[InMemoryCache__GetAsync (in memory)] - Cache Hit - {normalizedKey}.");
                    return value;
                }
            }

            Console.WriteLine("[InMemoryCache__GetAsync (in memory)] - Cache Miss.");
            return null;
        }

        /// <summary>
        /// Fetch multiple string keys in one round-trip when backed by Redis.
        /// Uses Redis MGET (StringGet with an array of keys) under the hood.
        ///
        /// Note: This is intentionally an additive method and does not require changing ICacheProvider.
        /// </summary>
        public async Task<IDictionary<string, string>> GetManyAsync(IEnumerable<string> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            var keyList = keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(NormalizeKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (keyList.Count == 0) return new Dictionary<string, string>();

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                var redisKeys = keyList.Select(k => (RedisKey)k).ToArray();
                var values = await db.StringGetAsync(redisKeys);

                var result = new Dictionary<string, string>(keyList.Count, StringComparer.Ordinal);

                for (var i = 0; i < keyList.Count; i++)
                {
                    result[keyList[i]] = (string)values[i];
                }

                return result;
            }

            lock (_inMemoryCache)
            {
                var result = new Dictionary<string, string>(keyList.Count, StringComparer.Ordinal);

                foreach (var key in keyList)
                {
                    result[key] = _inMemoryCache.TryGetValue(key, out var value) ? value : null;
                }

                return result;
            }
        }

        public async Task<IEnumerable<object>> GetCollection(string collectionKey)
        {
            if (string.IsNullOrWhiteSpace(collectionKey)) throw new ArgumentException("Collection key is required.", nameof(collectionKey));

            var normalizedCollectionKey = NormalizeKey(collectionKey);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                var result = await db.HashGetAllAsync(normalizedCollectionKey);
                return result.Cast<object>();
            }

            lock (_inMemoryCollections)
            {
                if (_inMemoryCollections.TryGetValue(normalizedCollectionKey, out var collection))
                {
                    return collection.Select(item => (object)item).ToList();
                }
            }

            return Enumerable.Empty<object>();
        }

        public async Task<string> GetFromCollection(string collectionKey, string key)
        {
            var normalizedCollectionKey = NormalizeKey(collectionKey);
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                _logger.Trace($"{this.Tag()} - Getting item with key: {normalizedKey} from collection: {normalizedCollectionKey}");

                var value = await db.HashGetAsync(normalizedCollectionKey, normalizedKey);
                return (string)value;
            }

            lock (_inMemoryCollections)
            {
                if (_inMemoryCollections.TryGetValue(normalizedCollectionKey, out var collection) &&
                    collection.TryGetValue(normalizedKey, out var value))
                {
                    return value;
                }
            }

            return null;
        }

        public Task RemoveAsync(string key)
        {
            ValidateKey(key);
            return RemoveAsync(key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        public Task RemoveFromCollectionAsync(string collectionKey, string key)
        {
            var normalizedCollectionKey = NormalizeKey(collectionKey);
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                _logger.Trace($"{this.Tag()} - Removing item with key: {normalizedKey} from collection: {normalizedCollectionKey}");
                return db.HashDeleteAsync(normalizedCollectionKey, normalizedKey);
            }

            lock (_inMemoryCollections)
            {
                if (_inMemoryCollections.TryGetValue(normalizedCollectionKey, out var collection))
                {
                    collection.Remove(normalizedKey);

                    if (collection.Count == 0)
                    {
                        _inMemoryCollections.Remove(normalizedCollectionKey);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            var normalizedKey = NormalizeKey(key);

            var json = await GetAsync(normalizedKey);

            if (String.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<T> GetAndDeleteAsync<T>(string key) where T : class
        {
            var normalizedKey = NormalizeKey(key);

            string json = null;

            if (_multiplexer != null)
            {
                var script = @"
local v = redis.call('GET', KEYS[1])
if v then redis.call('DEL', KEYS[1]) end
return v
";

                var db = _multiplexer.GetDatabase() ?? throw new ArgumentNullException("Database for cache provider is null.");
                json = (RedisValue)await db.ScriptEvaluateAsync(script, keys: new RedisKey[] { normalizedKey });
            }
            else
            {
                lock (_inMemoryCache)
                {
                    if (_inMemoryCache.TryGetValue(normalizedKey, out json))
                    {
                        _inMemoryCache.Remove(normalizedKey);
                    }
                }
            }

            await InvalidateDependentsAsync(normalizedKey);

            if (String.IsNullOrEmpty(json))
            {
                _logger.Trace($"{this.Tag()} - GetAndDeleteAsync Key: {normalizedKey} not found in cache.");
                return null;
            }

            _logger.Trace($"{this.Tag()} - GetAndDeleteAsync Key: {normalizedKey} found and removed from cache.");
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<long> GetLongAsync(string key)
        {
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase() ?? throw new ArgumentNullException("Database for cache provider is null.");
                var val = await db.StringGetAsync(normalizedKey).ConfigureAwait(false);

                if (val.IsNullOrEmpty)
                {
                    return 0;
                }

                if (long.TryParse(val.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }

                return 0;
            }

            lock (_inMemoryCache)
            {
                if (_inMemoryCache.TryGetValue(normalizedKey, out var value))
                {
                    return Convert.ToInt64(value);
                }
            }

            return 0;
        }

        public async Task<long> IncrementAsync(string key)
        {
            ValidateKey(key);
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase() ?? throw new ArgumentNullException("Database for cache provider is null.");
                return await db.StringIncrementAsync(normalizedKey).ConfigureAwait(false);
            }

            lock (_inMemoryCache)
            {
                if (_inMemoryCache.TryGetValue(normalizedKey, out var value))
                {
                    var intValue = Convert.ToInt64(value) + 1;
                    _inMemoryCache[normalizedKey] = intValue.ToString();
                    return intValue;
                }

                _inMemoryCache[normalizedKey] = "1";
                return 1;
            }
        }

        private Task AddCacheValueAsync(string key, string value, TimeSpan ttl)
        {
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                _logger.Trace($"{this.Tag()} - Added Key: {normalizedKey}");
                return db.StringSetAsync(normalizedKey, value, ttl, When.Always);
            }

            lock (_inMemoryCache)
            {
                _inMemoryCache[normalizedKey] = value;
            }

            return Task.CompletedTask;
        }

        private async Task RemoveAsync(string key, HashSet<string> visitedKeys)
        {
            var normalizedKey = NormalizeKey(key);

            if (!visitedKeys.Add(normalizedKey))
            {
                return;
            }

            var dependentKeys = await GetDependentCacheKeysAsync(normalizedKey);

            await RemoveCacheValueAsync(normalizedKey);

            foreach (var dependentKey in dependentKeys)
            {
                await RemoveAsync(dependentKey, visitedKeys);
            }

            await RemoveCacheValueAsync(GetDependentsCollectionKey(normalizedKey));
        }

        private Task InvalidateDependentsAsync(string key)
        {
            return InvalidateDependentsAsync(key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private async Task InvalidateDependentsAsync(string key, HashSet<string> visitedKeys)
        {
            var normalizedKey = NormalizeKey(key);

            if (!visitedKeys.Add(normalizedKey))
            {
                return;
            }

            var dependentKeys = await GetDependentCacheKeysAsync(normalizedKey);

            foreach (var dependentKey in dependentKeys)
            {
                await RemoveAsync(dependentKey, visitedKeys);
            }

            await RemoveCacheValueAsync(GetDependentsCollectionKey(normalizedKey));
        }

        private async Task<IReadOnlyCollection<string>> GetDependentCacheKeysAsync(string key)
        {
            var dependentsCollectionKey = GetDependentsCollectionKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                var values = await db.HashValuesAsync(dependentsCollectionKey);

                return values
                    .Where(value => value.HasValue)
                    .Select(value => NormalizeKey(value.ToString()))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            lock (_inMemoryCollections)
            {
                if (_inMemoryCollections.TryGetValue(dependentsCollectionKey, out var collection))
                {
                    return collection.Values
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(NormalizeKey)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }

            return Array.Empty<string>();
        }

        private Task RemoveCacheValueAsync(string key)
        {
            var normalizedKey = NormalizeKey(key);

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                if (db == null)
                {
                    throw new ArgumentNullException("Database for cache provider is null.");
                }

                _logger.Trace($"{this.Tag()} - Removing item with key: {normalizedKey}");
                return db.KeyDeleteAsync(normalizedKey);
            }

            lock (_inMemoryCache)
            {
                _inMemoryCache.Remove(normalizedKey);
            }

            lock (_inMemoryCollections)
            {
                _inMemoryCollections.Remove(normalizedKey);
            }

            return Task.CompletedTask;
        }

        private static string GetDependentsCollectionKey(string key)
        {
            return $"{NormalizeKey(key)}{DependentsSuffix}";
        }

        private static string NormalizeKey(string key)
        {
            ValidateKey(key);
            return key.Trim().ToLowerInvariant();
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key is required.", nameof(key));
            }
        }
    }
}
