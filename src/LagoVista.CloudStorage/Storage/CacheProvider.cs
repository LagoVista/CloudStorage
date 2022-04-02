using System;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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
			else
            {
				Console.WriteLine("Not using cache.");
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

		public Task AddToCollectionAsync(string collectionKey, string key, string value)
		{
			if (_multiplexer != null)
			{
				var db = _multiplexer.GetDatabase();
				return db.HashSetAsync(collectionKey, key, value);
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

		public async Task<IEnumerable<object>> GetCollection(string collectionKey)
		{
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
			if (_multiplexer != null)
			{
				var db = _multiplexer.GetDatabase();
				if(db == null)
                {
					throw new ArgumentNullException("Database for cache provider is null.");
                }

				Console.WriteLine($"Removing item with key: {key}");
				return db.KeyDeleteAsync(key);
			}

			return Task.CompletedTask;
		}

		public Task RemoveFromCollectionAsync(string collectionKey, string key)
		{
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