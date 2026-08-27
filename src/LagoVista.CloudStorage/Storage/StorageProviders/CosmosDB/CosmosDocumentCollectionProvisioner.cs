using LagoVista.CloudStorage.DocumentDB;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class CosmosDocumentCollectionProvisioner
    {
        private readonly DocumentCollectionProvisioningCache _cache;

        public CosmosDocumentCollectionProvisioner(DocumentCollectionProvisioningCache cache = null)
        {
            _cache = cache ?? new DocumentCollectionProvisioningCache();
        }

        public Task EnsureExistsAsync(CosmosClient client, string endpoint, string databaseName, string collectionName, string partitionKeyPath, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (String.IsNullOrWhiteSpace(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (String.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (String.IsNullOrWhiteSpace(partitionKeyPath)) throw new ArgumentNullException(nameof(partitionKeyPath));

            var cacheKey = $"cosmos|{endpoint}|{databaseName}|{collectionName}|{partitionKeyPath}";
            return _cache.EnsureAsync(cacheKey, async () =>
            {
                var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: cancellationToken).ConfigureAwait(false);
                var response = await databaseResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(collectionName, partitionKeyPath), cancellationToken: cancellationToken).ConfigureAwait(false);

                var actualPartitionKeyPath = response.Resource?.PartitionKeyPath;
                if (!String.Equals(actualPartitionKeyPath, partitionKeyPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Cosmos container '{databaseName}/{collectionName}' uses partition key '{actualPartitionKeyPath}', expected '{partitionKeyPath}'.");
                }
            });
        }
    }
}
