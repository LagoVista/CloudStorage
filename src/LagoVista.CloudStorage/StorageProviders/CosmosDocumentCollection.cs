using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
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
    public sealed class CosmosDocumentCollection : IDocumentCollection
    {
        private readonly ICosmosClientProvider _cosmosClientProvider;
        private readonly string _endpoint;
        private readonly string _sharedKey;
        private readonly string _databaseName;
        private readonly string _collectionName;

        public CosmosDocumentCollection(ICosmosClientProvider cosmosClientProvider, string endpoint, string sharedKey, string databaseName, string collectionName)
        {
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _sharedKey = sharedKey ?? throw new ArgumentNullException(nameof(sharedKey));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        }

        public async Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TDocument>().Where(query);
            if (sort != null) linqQuery = linqQuery.OrderBy(sort);
            linqQuery = linqQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize);

            var items = new List<TDocument>();
            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    items.AddRange(response);
                }
            }

            return ListResponse<TDocument>.Create(listRequest, items);
        }

        public async Task<ListResponse<TProjection>> QueryAsync<TDocument, TProjection, TSort>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, Expression<Func<TDocument, TSort>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TDocument>().Where(query);
            if (sort != null) linqQuery = linqQuery.OrderBy(sort);

            var projectedQuery = linqQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize).Select(projection);
            var items = new List<TProjection>();
            using (var iterator = projectedQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    items.AddRange(response);
                }
            }

            return ListResponse<TProjection>.Create(listRequest, items);
        }

        public async Task<IEnumerable<TProjection>> QueryAsync<TDocument, TProjection>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (projection == null) throw new ArgumentNullException(nameof(projection));

            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var projectedQuery = container.GetItemLinqQueryable<TDocument>().Where(query).Select(projection);
            var items = new List<TProjection>();
            using (var iterator = projectedQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    items.AddRange(response);
                }
            }

            return items;
        }

        public async Task<IEnumerable<TResult>> QueryAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var query = CreateQueryDefinition(request);
            var client = _cosmosClientProvider.GetClient(_endpoint, _sharedKey);
            var container = client.GetContainer(_databaseName, _collectionName);
            var iterator = container.GetItemQueryIterator<TResult>(query);
            var items = new List<TResult>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                items.AddRange(response);
            }

            return items;
        }

        private static QueryDefinition CreateQueryDefinition(DocumentQueryRequest request)
        {
            switch (request.QueryType)
            {
                case DocumentQueryType.CustomerIndustryNicheSalesStageCounts:
                    return new QueryDefinition("SELECT c.Industry, c.IndustryNiche, c.SalesStage, COUNT(c.id) AS CountLeads FROM c WHERE c.EntityType = 'CustomerEntity' AND c.OwnerOrganization.Id = @orgId GROUP BY c.Industry, c.IndustryNiche, c.SalesStage").WithParameter("@orgId", request.GetRequired<string>("orgId"));

                default:
                    throw new NotSupportedException($"Registered document query '{request.QueryType}' is not implemented by the Cosmos provider.");
            }
        }
    }
}
