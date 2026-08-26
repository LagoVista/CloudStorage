using LagoVista.CloudStorage.Interfaces;
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
        public async Task<ListResponse<TEntity>> QueryAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> sort, ListRequest listRequest, bool descending) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (sort == null) throw new ArgumentNullException(nameof(sort));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var linqQuery = GetCollection<TEntity>().AsQueryable().Where(query).Where(item => item.EntityType == typeof(TEntity).Name);
            if (!listRequest.ShowDeleted) linqQuery = linqQuery.Where(item => !item.IsDeleted.HasValue || !item.IsDeleted.Value);
            if (!listRequest.ShowDrafts) linqQuery = linqQuery.Where(item => item.IsDraft != true);

            var orderedQuery = descending ? linqQuery.OrderByDescending(sort) : linqQuery.OrderBy(sort);
            var items = await orderedQuery.Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Take(listRequest.PageSize).ToListAsync().ConfigureAwait(false);
            return ListResponse<TEntity>.Create(listRequest, items);
        }
    }
}