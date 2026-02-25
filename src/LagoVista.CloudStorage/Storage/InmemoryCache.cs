using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class InmemoryCache : ICacheProvider
    {
        private static Dictionary<string, string> _inMemoryCache = new Dictionary<string, string>();
        private readonly IAdminLogger _logger;

        public InmemoryCache(IAdminLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task AddAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            return AddAsync(key, JsonConvert.SerializeObject(value), ttl);
        }

        public Task AddAsync(string key, string value, TimeSpan? ttl = null)
        {
            lock (_inMemoryCache)
            {
                if (_inMemoryCache.ContainsKey(key))
                    _inMemoryCache.Remove(key);

                _inMemoryCache.Add(key, value);
                _logger.Trace($"{this.Tag()} - Added to cache - {key}");
            }

            return Task.CompletedTask;
        }

        public Task AddToCollectionAsync(string collectionKey, string key, string value)
        {
            throw new NotImplementedException();
        }

        public async Task<T> GetAndDeleteAsync<T>(string key) where T : class
        {
            var result = await GetAsync<T>(key);
            if(result != null)
            {
                await RemoveAsync(key);
            }

            return result;
        }

        public Task<string> GetAsync(string key)
        {
            if (_inMemoryCache.ContainsKey(key))
            {
                _logger.Trace($"{this.Tag()} - Cache Hit - {key}.");
                return Task.FromResult( _inMemoryCache[key]);
            }
            else
            {
                _logger.Trace($"{this.Tag()} - Cache Miss - {key}.");
            }

            return null;
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            var json = await GetAsync(key);
            if (!String.IsNullOrEmpty(json))
                return JsonConvert.DeserializeObject<T>(json);

            return null;
        }

        public Task<IEnumerable<object>> GetCollection(string collectionKey)
        {
            return null;
        }

        public Task<string> GetFromCollection(string collectionKey, string key)
        {
            return null;
        }

        public Task<long> GetLongAsync(string key)
        {
            if (_inMemoryCache.ContainsKey(key))
            {
                var intValue = Convert.ToInt64(_inMemoryCache[key]);
                return Task.FromResult(intValue);
            }

            return Task.FromResult((long)0);
        }

        public Task<IDictionary<string, string>> GetManyAsync(IEnumerable<string> keys)
        {
            return null;
        }

        public Task<long> IncrementAsync(string key)
        {
            if (_inMemoryCache.ContainsKey(key))
            {
                var intValue = Convert.ToInt64(_inMemoryCache[key]);
                _inMemoryCache.Remove(key);
                intValue++;
                _inMemoryCache.Add(key, intValue.ToString());

                return Task.FromResult(intValue);
            }

            _inMemoryCache.Add(key, "1");
            return Task.FromResult((long)1);
        }

        public Task RemoveAsync(string key)
        {
            _inMemoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveFromCollectionAsync(string collectionKey, string key)
        {
            return Task.CompletedTask;
        }
    }
}
