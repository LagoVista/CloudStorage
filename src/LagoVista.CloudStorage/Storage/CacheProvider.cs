using System;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class CacheProvider : ICacheProvider
    {
        private readonly ConnectionMultiplexer _multiplexer;


        public CacheProvider(ICacheProviderSettings settings)
        {
             _multiplexer = ConnectionMultiplexer.Connect(settings.CacheSettings.Uri);
        }

        public Task AddAsync(string key, string value)
        {
            Console.WriteLine($"Added cache item: {key}");
            var db = _multiplexer.GetDatabase();
            return db.StringSetAsync(key, value);
        }

        public async Task<string> GetAsync(string key)
        {
            var db = _multiplexer.GetDatabase();
            var result = await db.StringGetAsync(key);

            Console.WriteLine($"Getting cache item: {key} found in cache: {!String.IsNullOrEmpty(result)}");
            return (string)result;
        }

        public Task RemoveAsync(string key)
        {
            var db = _multiplexer.GetDatabase();

            Console.WriteLine($"Removing item with key: {key}");
            return db.KeyDeleteAsync(key);
        }
    }
}
