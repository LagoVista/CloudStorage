// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 9c0fe2a5af49bcbbde0c9a83c13851c08b24f2f10ba1681ae41a094f6cb7674b
// IndexVersion: 1
// --- END CODE INDEX META ---
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
