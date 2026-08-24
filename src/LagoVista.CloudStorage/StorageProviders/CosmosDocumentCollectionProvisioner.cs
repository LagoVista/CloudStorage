using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Storage;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class CosmosDocumentCollectionProvisioner
    {
        private readonly CosmosClientProvider _cosmosClientProvider;
        private readonly DocumentCollectionProvisioningCache _cache;

        public CosmosDocumentCollectionProvisioner(CosmosClientProvider cosmosClientProvider, DocumentCollectionProvisioningCache cache = null)
        {
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            _cache = cache ?? new DocumentCollectionProvisioningCache();
        }

        public Task EnsureExistsAsync(string endpoint, string sharedKey, string databaseName, string collectionName, string partitionKeyPath, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (String.IsNullOrWhiteSpace(sharedKey)) throw new ArgumentNullException(nameof(sharedKey));
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (String.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (String.IsNullOrWhiteSpace(partitionKeyPath)) throw new ArgumentNullException(nameof(partitionKeyPath));

            var cacheKey = $"cosmos|{endpoint}|{databaseName}|{collectionName}|{partitionKeyPath}";
            return _cache.EnsureAsync(cacheKey, async () =>
            {
                var client = _cosmosClientProvider.GetClient(endpoint, sharedKey);
                var database = client.GetDatabase(databaseName);
                var response = await database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(collectionName, partitionKeyPath),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var actualPartitionKeyPath = response.Resource?.PartitionKeyPath;
                if (!String.Equals(actualPartitionKeyPath, partitionKeyPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Cosmos container '{databaseName}/{collectionName}' uses partition key '{actualPartitionKeyPath}', expected '{partitionKeyPath}'.");
                }
            });
        }
    }
}
