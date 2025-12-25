// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 99303d3fab421b6f93933bc15b8fc4b699195eb35454072b3772cebbf79f70de
// IndexVersion: 2
// --- END CODE INDEX META ---
using System;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using LagoVista.Core.Interfaces;

namespace LagoVista.CloudStorage.Storage
{
    public class CacheProvider : ICacheProvider
    {
        private readonly ConnectionMultiplexer _multiplexer = null;

        private static Dictionary<string, string> _inMemoryCache = null;

        public CacheProvider(ICacheProviderSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings.UseCache)
            {
                _multiplexer = ConnectionMultiplexer.Connect(settings.CacheSettings.Uri);
            }
        }

        public Task AddAsync(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                Console.WriteLine($"[RemoteCache__AddAsync] Key: {key}");

                return db.StringSetAsync(key, value);
            }
            else if (_inMemoryCache != null)
            {
                if (_inMemoryCache.ContainsKey(key))
                    _inMemoryCache.Remove(key);

                _inMemoryCache.Add(key, value);
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
                Console.WriteLine($"[RemoteCache__GetAsync] Getting cache item: {key} found in cache: {!String.IsNullOrEmpty(str)}");
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
                Console.WriteLine($"Getting cache collection: {collectionKey} found in cache: {await db.KeyExistsAsync(collectionKey) && await db.KeyTypeAsync(collectionKey) == RedisType.Hash}");

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
                Console.WriteLine($"Getting cache collection item: {collectionKey} found in cache: {await db.HashExistsAsync(collectionKey, key)}");

                var result = await db.HashGetAsync(collectionKey, key);
                return (string)result;
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

                Console.WriteLine($"Removing item with key: {key}");
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

                Console.WriteLine($"Removing item with key: {key} from collection: {collectionKey}");
                return db.HashDeleteAsync(collectionKey, key);
            }

            return Task.CompletedTask;
        }
    }
}
