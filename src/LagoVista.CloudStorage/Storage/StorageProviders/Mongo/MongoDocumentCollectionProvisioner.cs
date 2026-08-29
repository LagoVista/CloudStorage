using LagoVista;
using LagoVista.CloudStorage.Storage.StorageProviders;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    [CriticalCoverage]
    public sealed class MongoDocumentCollectionProvisioner
    {
        private static readonly ConcurrentDictionary<string, MongoClient> _clients = new ConcurrentDictionary<string, MongoClient>(StringComparer.Ordinal);
        private readonly DocumentCollectionProvisioningCache _cache;

        public MongoDocumentCollectionProvisioner(DocumentCollectionProvisioningCache cache = null)
        {
            _cache = cache ?? new DocumentCollectionProvisioningCache();
        }

        public Task EnsureExistsAsync(string connectionString, string databaseName, string collectionName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (String.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));

            var cacheKey = $"mongo|{connectionString}|{databaseName}|{collectionName}";
            return _cache.EnsureAsync(cacheKey, async () =>
            {
                var client = _clients.GetOrAdd(connectionString, value => new MongoClient(value));
                var database = client.GetDatabase(databaseName);
                try
                {
                    await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (MongoCommandException ex) when (ex.Code == 48 || String.Equals(ex.CodeName, "NamespaceExists", StringComparison.OrdinalIgnoreCase))
                {
                    // Another process or pod created the collection first. This is the expected first-use race.
                }
            });
        }
    }
}
