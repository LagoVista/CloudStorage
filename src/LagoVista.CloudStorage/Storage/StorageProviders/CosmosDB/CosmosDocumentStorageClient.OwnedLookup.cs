using LagoVista.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class CosmosDocumentStorageClient
    {
        public async Task<TProjection> GetOwnedDocumentProjectionAsync<TProjection>(string id, string ownerOrganizationId, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));
            if (String.IsNullOrWhiteSpace(ownerOrganizationId)) throw new ArgumentException("Owner organization id is required.", nameof(ownerOrganizationId));

            var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.id = @id AND c.OwnerOrganization.Id = @ownerOrganizationId").WithParameter("@id", id.Trim()).WithParameter("@ownerOrganizationId", ownerOrganizationId);
            using var iterator = GetRawDocumentContainer().GetItemQueryIterator<TProjection>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in response) return item;
            }

            if (throwOnNotFound) throw new RecordNotFoundException(typeof(TProjection).Name, id);
            return null;
        }
    }
}