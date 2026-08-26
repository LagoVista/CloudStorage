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
        public async Task<ListResponse<TEntityFactory>> QuerySummaryAsync<TEntityFactory>(string entityType, Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest, bool descending) where TEntityFactory : class, ISummaryFactory, INoSQLEntity, ICategorized, IAuditableEntity
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var linqQuery = GetContainer<TEntityFactory>().GetItemLinqQueryable<TEntityFactory>().Where(query).Where(item => item.EntityType == entityType && (listRequest.ShowDeleted || item.IsDeleted.IsNull() || !item.IsDeleted.HasValue || !item.IsDeleted.Value) && (listRequest.ShowDrafts || !item.IsDraft.IsDefined() || item.IsDraft == false));
            if (!String.IsNullOrWhiteSpace(listRequest.CategoryKey)) linqQuery = linqQuery.Where(item => item.Category.Key == listRequest.CategoryKey);
            if (sort != null) linqQuery = descending ? linqQuery.OrderByDescending(sort) : linqQuery.OrderBy(sort);

            linqQuery = linqQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize);
            var items = new List<TEntityFactory>();
            using (var iterator = linqQuery.ToFeedIterator())
            {
                while (iterator.HasMoreResults) items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return ListResponse<TEntityFactory>.Create(listRequest, items);
        }
    }
}