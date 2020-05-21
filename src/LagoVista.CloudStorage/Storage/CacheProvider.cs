using System;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class CacheProvider : ICacheProvider
    {
        private readonly ConnectionMultiplexer _multiplexer = null;


        public CacheProvider(ICacheProviderSettings settings)
        {
            if (settings.UseCache)
            {
                _multiplexer = ConnectionMultiplexer.Connect(settings.CacheSettings.Uri);
            }
        }

        public Task AddAsync(string key, string value)
        {
            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                return db.StringSetAsync(key, value);
            }

            return Task.CompletedTask;
        }

        public async Task<string> GetAsync(string key)
        {
            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();
                var result = await db.StringGetAsync(key);

                Console.WriteLine($"Getting cache item: {key} found in cache: {!String.IsNullOrEmpty(result)}");
                return (string)result;
            }
            return null;
        }

        public Task RemoveAsync(string key)
        {
            if (_multiplexer != null)
            {
                var db = _multiplexer.GetDatabase();

                Console.WriteLine($"Removing item with key: {key}");
                return db.KeyDeleteAsync(key);
            }
            return null;
        }
    }
}
