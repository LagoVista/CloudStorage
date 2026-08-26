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
        public async Task<ListResponse<TEntity>> QueryAllAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> sort, ListRequest listRequest, bool descending) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (sort == null) throw new ArgumentNullException(nameof(sort));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var baseQuery = GetContainer<TEntity>().GetItemLinqQueryable<TEntity>().Where(query);
            var orderedQuery = descending ? baseQuery.OrderByDescending(sort) : baseQuery.OrderBy(sort);
            var linqQuery = orderedQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize);
            var items = new List<TEntity>();

            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults) items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return ListResponse<TEntity>.Create(listRequest, items);
        }
    }
}
