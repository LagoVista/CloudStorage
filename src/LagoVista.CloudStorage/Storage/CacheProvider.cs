
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
        private readonly IAdminLogger _logger;
        private readonly ConnectionMultiplexer _multiplexer = null;

        private static Dictionary<string, string> _inMemoryCache = new Dictionary<string, string>();


        public CacheProvider(ICacheProviderSettings settings, IAdminLogger adminlogger)
        {
            _logger = adminlogger ?? throw new ArgumentNullException(nameof(adminlogger));

            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings.UseCache)
            {
                try
                {
                    _logger.Trace($"{this.Tag()} - Initializing cache provider with settings: {JsonConvert.SerializeObject(settings.CacheSettings.Uri)}");
                    _multiplexer = ConnectionMultiplexer.Connect(settings.CacheSettings.Uri);
                    _logger.Trace($"{this.Tag()} - Established connection to REDIS: {JsonConvert.SerializeObject(settings.CacheSettings.Uri)}");
                }
                catch (Exception ex)
                {
                    _logger.AddError(this.Tag(), $"Failed to connect to REDIS with settings: {JsonConvert.SerializeObject(settings.CacheSettings.Uri)}. Exception: {ex}.  You can disable remote cache by setting UseCache = false in AppSettings.  Or you can setup a SSH tunnel to dev cache server.");
                    throw;
                }

            }
        }

        public Task AddAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            if(value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return AddAsync(key, JsonConvert.SerializeObject(value), ttl);
        }

        public Task AddAsync(string key, string value, TimeSpan? ttl = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                _logger.Trace($"{this.Tag()} - Added Key: {key}");

                return db.StringSetAsync(key, value, ttl, When.Always);
            }
            else if (_inMemoryCache != null)
            {
                lock (_inMemoryCache)
                {
                    if (_inMemoryCache.ContainsKey(key))
                        _inMemoryCache.Remove(key);

                    _inMemoryCache.Add(key, value);
                }
            }

            return Task.CompletedTask;
        }

        public Task AddToCollectionAsync(string collectionKey, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(collectionKey)) throw new ArgumentException("Collection key is required.", nameof(collectionKey));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                return db.HashSetAsync(collectionKey, key, value);
            }

            return Task.CompletedTask;
        }

        public async Task<string> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                var result = await db.StringGetAsync(key);

                var str = (string)result;
                if(!result.HasValue)
                {
                    _logger.Trace($"{this.Tag()} - Getting cache item: {key} found in cache: {!String.IsNullOrEmpty(str)}");
                }
                else 
                    _logger.Trace($"{this.Tag()} - Getting cache item: {key} found in cache: {!String.IsNullOrEmpty(str)}");
                return str;
            }
            else
            {
                if (_inMemoryCache != null)
                {
                    if (_inMemoryCache.ContainsKey(key))
                    {
                        Console.WriteLine($"[InMemoryCache__GetAsync (in memory)] - Cache Hit - {key}.");
                        return _inMemoryCache[key];
                    }
                    else
                    {
                        Console.WriteLine("[InMemoryCache__GetAsync (in memory)] - Cache Miss.");
                    }
                }

            }
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

            var keyList = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
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

            if (_inMemoryCache != null)
            {
                var result = new Dictionary<string, string>(keyList.Count, StringComparer.Ordinal);
                foreach (var k in keyList)
                {
                    result[k] = _inMemoryCache.ContainsKey(k) ? _inMemoryCache[k] : null;
                }

                return result;
            }

            return new Dictionary<string, string>();
        }

        public async Task<IEnumerable<object>> GetCollection(string collectionKey)
        {
            if (string.IsNullOrWhiteSpace(collectionKey)) throw new ArgumentException("Collection key is required.", nameof(collectionKey));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                var result = await db.HashGetAllAsync(collectionKey);
                return result.Cast<object>();
            }
            return null;
        }

        public async Task<string> GetFromCollection(string collectionKey, string key)
        {
            if (string.IsNullOrWhiteSpace(collectionKey)) throw new ArgumentException("Collection key is required.", nameof(collectionKey));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                _logger.Trace($"{this.Tag()} - Getting item with key: {key} from collection: {collectionKey}");

                var value = await db.HashGetAsync(collectionKey, key);

                var result = (string)value;


                return result;
            }

            return null;
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                if (db == null)
                {
                    throw new ArgumentNullException("Database for cache provider is null.");
                }

                _logger.Trace($"{this.Tag()} - Removing item with key: {key}");
                return db.KeyDeleteAsync(key);
            }
            else if (_inMemoryCache != null)
            {
                _inMemoryCache.Remove(key);
            }

            return Task.CompletedTask;
        }

        public Task RemoveFromCollectionAsync(string collectionKey, string key)
        {
            if (string.IsNullOrWhiteSpace(collectionKey)) throw new ArgumentException("Collection key is required.", nameof(collectionKey));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                _logger.Trace($"{this.Tag()} - Removing item with key: {key} from collection: {collectionKey}");
                return db.HashDeleteAsync(collectionKey, key);
            }

            return Task.CompletedTask;
        }

        public async Task<T> GetAsync<T>(string key) where T: class
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            var json = await GetAsync(key);
            if (String.IsNullOrEmpty(json))
                return null;

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<T> GetAndDeleteAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var script = @"
local v = redis.call('GET', KEYS[1])
if v then redis.call('DEL', KEYS[1]) end
return v
";

                var db = _multiplexer.GetDatabase() ?? throw new ArgumentNullException("Database for cache provider is null.");
                var json = (RedisValue)await db.ScriptEvaluateAsync(
                            script,
                            keys: new RedisKey[] { key });


                if (String.IsNullOrEmpty(json))
                {
                    _logger.Trace($"{this.Tag()} - GetAndDeleteAsync Key: {key} not found in cache.");  
                    return null;
                }

                _logger.Trace($"{this.Tag()} - GetAndDeleteAsync Key: {key} found and removed from cache.");

                return JsonConvert.DeserializeObject<T>(json);
            }
            else if (_inMemoryCache != null)
            {
                if(_inMemoryCache.ContainsKey(key))
                {
                    var json = _inMemoryCache[key];
                    _inMemoryCache.Remove(key);
                    return JsonConvert.DeserializeObject<T>(json);
                }

                return null;
            }

            return null;
        }

        public async Task<long> GetLongAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase() ?? throw new ArgumentNullException("Database for cache provider is null.");

                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException(nameof(key));

                var val = await db.StringGetAsync(key).ConfigureAwait(false);
                if (val.IsNullOrEmpty) return 0;

                // Redis returns bytes/string. Be lenient: if it isn't a number, treat as 0 (cache-safe).
                if (long.TryParse(val.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;

                return 0;
            }
            else if (_inMemoryCache != null)
            {
                if (_inMemoryCache.ContainsKey(key))
                {
                    var intValue = Convert.ToInt64(_inMemoryCache[key]);
                    return intValue;
                }

                return 0;
            }

            return 0;
        }

        public async Task<long> IncrementAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase() ?? throw new ArgumentNullException("Database for cache provider is null.");
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException(nameof(key));

                // Atomic INCR (creates key if missing, starting at 0 then +1)
                return await db.StringIncrementAsync(key).ConfigureAwait(false);
            }
            else if (_inMemoryCache != null)
            {
                if (_inMemoryCache.ContainsKey(key))
                {
                    var intValue = Convert.ToInt64( _inMemoryCache[key]);
                    _inMemoryCache.Remove(key);
                    intValue++;
                    _inMemoryCache.Add(key, intValue.ToString());

                    return intValue;
                }

                _inMemoryCache.Add(key, "1");
                return 1;
            }

            return 1;
        }
    }
}
