using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Utils
{
    public class LocalCacheProvider : ICacheProvider
    {
        public string _host;

        public LocalCacheProvider(string host)
        {
            _host = host;
        }

        public Task AddAsync(string key, string value)
        {
            return Task.CompletedTask;
        }

        public Task AddToCollectionAsync(string collectionKey, string key, string value)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetAsync(string key)
        {
            return Task<string>.FromResult((string)null);
        }

        public Task<IEnumerable<object>> GetCollection(string collectionKey)
        {
            return Task<string>.FromResult((IEnumerable<object>)null);
        }

        public Task<string> GetFromCollection(string collectionKey, string key)
        {
            return Task<string>.FromResult((string)null);
        }

        public async Task RemoveAsync(string key)
        {
            using (var client = new HttpClient())
            {
                var url = $"{_host}/api/core/cache/clear/{key}";
                Console.WriteLine($"Remove Cache with url: {url}");
                var result = await client.GetAsync(url);
                result.EnsureSuccessStatusCode();

            }
            
        }

        public Task RemoveFromCollectionAsync(string collectionKey, string key)
        {
            return Task.CompletedTask;
        }
    }
}
