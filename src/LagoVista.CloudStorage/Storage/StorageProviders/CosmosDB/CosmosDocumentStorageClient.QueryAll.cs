using LagoVista.CloudStorage.Models;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class CosmosDocumentStorageClient
    {
        public async Task<ListResponse<TEntity>> QueryAllAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var linqQuery = GetContainer<TEntity>().GetItemLinqQueryable<TEntity>()
                .Where(query)
                .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                .Take(listRequest.PageSize);

            var items = new List<TEntity>();
            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                    items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return ListResponse<TEntity>.Create(listRequest, items);
        }
    }
}
