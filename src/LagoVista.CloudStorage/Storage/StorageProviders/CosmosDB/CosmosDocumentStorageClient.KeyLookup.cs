using LagoVista.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class CosmosDocumentStorageClient
    {
        public async Task<TProjection> GetDocumentProjectionByKeyAsync<TProjection>(string entityType, string key, string ownerOrganizationId, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentException("Document key is required.", nameof(key));
            if (String.IsNullOrWhiteSpace(ownerOrganizationId)) throw new ArgumentException("Owner organization id is required.", nameof(ownerOrganizationId));

            var collectionResolver = new DocumentCollectionNameResolver();
            if (!collectionResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName)) throw new InvalidOperationException($"Could not resolve Cosmos collection for entity type '{entityType}'.");

            var container = _cosmosClientProvider.GetClient(_settings.Endpoint, _settings.AccessKey).GetContainer(_settings.DatabaseName, collectionName);
            var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.EntityType = @entityType AND c.Key = @key AND c.OwnerOrganization.Id = @ownerOrganizationId").WithParameter("@entityType", entityType).WithParameter("@key", key.Trim()).WithParameter("@ownerOrganizationId", ownerOrganizationId);
            using var iterator = container.GetItemQueryIterator<TProjection>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in response) return item;
            }

            if (throwOnNotFound) throw new RecordNotFoundException(entityType, key);
            return null;
        }
    }
}