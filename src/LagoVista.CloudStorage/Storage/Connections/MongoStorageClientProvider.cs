using MongoDB.Driver;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public interface IMongoStorageClientProvider
    {
        MongoStorageSettings Settings { get; }
        IMongoClient Client { get; }
        IMongoDatabase GetDatabase(string databaseName = null);
    }

    public sealed class MongoStorageClientProvider : IMongoStorageClientProvider
    {
        private readonly Lazy<IMongoClient> _client;

        public MongoStorageClientProvider(MongoStorageSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _client = new Lazy<IMongoClient>(() => new MongoClient(Settings.ConnectionString), true);
        }

        public MongoStorageSettings Settings { get; }

        public IMongoClient Client => _client.Value;

        public IMongoDatabase GetDatabase(string databaseName = null)
        {
            var resolvedDatabaseName = String.IsNullOrWhiteSpace(databaseName)
                ? Settings.DefaultDatabaseName
                : databaseName;

            if (String.IsNullOrWhiteSpace(resolvedDatabaseName))
            {
                throw new InvalidOperationException("A Mongo database name must be supplied explicitly or configured as DefaultDatabaseName.");
            }

            return Client.GetDatabase(resolvedDatabaseName);
        }
    }
}
