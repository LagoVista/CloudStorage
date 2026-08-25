using MongoDB.Driver;
using System;
using System.Collections.Concurrent;

namespace LagoVista.CloudStorage.Storage
{
    public interface IMongoStorageClientFactory
    {
        IMongoClient GetClient(string connectionString);
        IMongoDatabase GetDatabase(string connectionString, string databaseName);
    }

    public sealed class MongoStorageClientFactory : IMongoStorageClientFactory
    {
        private readonly ConcurrentDictionary<string, IMongoClient> _clients =
            new ConcurrentDictionary<string, IMongoClient>(StringComparer.Ordinal);

        public IMongoClient GetClient(string connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            return _clients.GetOrAdd(connectionString, value => new MongoClient(value));
        }

        public IMongoDatabase GetDatabase(string connectionString, string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));

            return GetClient(connectionString).GetDatabase(databaseName);
        }
    }
}
