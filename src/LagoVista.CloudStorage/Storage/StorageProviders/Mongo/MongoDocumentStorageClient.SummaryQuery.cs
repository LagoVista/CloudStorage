using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class MongoDocumentStorageClient
    {
        public async Task<ListResponse<TEntityFactory>> QuerySummaryAsync<TEntityFactory>(string entityType, Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest, bool descending) where TEntityFactory : class, ISummaryFactory, INoSQLEntity, ICategorized, IAuditableEntity
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var linqQuery = GetCollection<TEntityFactory>().AsQueryable().Where(query).Where(item => item.EntityType == entityType);
            if (!String.IsNullOrWhiteSpace(listRequest.CategoryKey)) linqQuery = linqQuery.Where(item => item.Category.Key == listRequest.CategoryKey);
            if (!listRequest.ShowDeleted) linqQuery = linqQuery.Where(item => !item.IsDeleted.HasValue || !item.IsDeleted.Value);
            if (!listRequest.ShowDrafts) linqQuery = linqQuery.Where(item => item.IsDraft != true);

            var orderedQuery = sort == null ? linqQuery : descending ? linqQuery.OrderByDescending(sort) : linqQuery.OrderBy(sort);
            var items = await orderedQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
            return ListResponse<TEntityFactory>.Create(listRequest, items);
        }
    }
}