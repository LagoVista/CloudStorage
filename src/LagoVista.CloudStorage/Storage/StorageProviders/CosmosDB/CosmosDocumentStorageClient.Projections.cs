using LagoVista.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class CosmosDocumentStorageClient
    {
        public async Task<TProjection> GetDocumentProjectionAsync<TProjection>(string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));

            var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            using var iterator = GetContainer<TProjection>().GetItemQueryIterator<TProjection>(
                query,
                requestOptions: new QueryRequestOptions { MaxItemCount = 1 });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                var projection = response.Resource.FirstOrDefault();
                if (projection != null) return projection;
            }

            if (throwOnNotFound) throw new RecordNotFoundException(typeof(TProjection).Name, id);
            return null;
        }

        public async Task<TProjection> GetDocumentProjectionAsync<TProjection>(string entityType, string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));

            var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.id = @id AND c.EntityType = @entityType")
                .WithParameter("@id", id)
                .WithParameter("@entityType", entityType);

            using var iterator = GetContainer<TProjection>().GetItemQueryIterator<TProjection>(
                query,
                requestOptions: new QueryRequestOptions { MaxItemCount = 1 });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                var projection = response.Resource.FirstOrDefault();
                if (projection != null) return projection;
            }

            if (throwOnNotFound) throw new RecordNotFoundException(entityType, id);
            return null;
        }

        public async Task<IEnumerable<TProjection>> GetDocumentProjectionsAsync<TProjection>(string entityType, Expression<Func<TProjection, bool>> query, CancellationToken cancellationToken = default)
            where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (query == null) throw new ArgumentNullException(nameof(query));

            var entityTypeProperty = typeof(TProjection).GetProperty("EntityType");
            if (entityTypeProperty == null || entityTypeProperty.PropertyType != typeof(string))
                throw new InvalidOperationException($"Projection type '{typeof(TProjection).Name}' must expose a string EntityType property for Cosmos projection queries.");

            var parameter = Expression.Parameter(typeof(TProjection), "item");
            var entityTypeFilter = Expression.Lambda<Func<TProjection, bool>>(
                Expression.Equal(Expression.Property(parameter, entityTypeProperty), Expression.Constant(entityType)),
                parameter);

            var items = new List<TProjection>();
            var linqQuery = GetContainer<TProjection>()
                .GetItemLinqQueryable<TProjection>()
                .Where(entityTypeFilter)
                .Where(query);

            using var iterator = linqQuery.ToFeedIterator();
            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false));

            return items;
        }
    }
}