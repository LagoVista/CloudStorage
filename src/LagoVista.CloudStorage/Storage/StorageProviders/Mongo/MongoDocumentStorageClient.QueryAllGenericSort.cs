using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using MongoDB.Driver;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class MongoDocumentStorageClient
    {
        public async Task<ListResponse<TEntity>> QueryAllAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> sort, ListRequest listRequest, bool descending) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (sort == null) throw new ArgumentNullException(nameof(sort));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var sortField = new ExpressionFieldDefinition<TEntity, TKey>(sort);
            var sortDefinition = descending ? Builders<TEntity>.Sort.Descending(sortField) : Builders<TEntity>.Sort.Ascending(sortField);

            var items = await GetCollection<TEntity>().Find(query).Sort(sortDefinition).Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize).Limit(listRequest.PageSize).ToListAsync().ConfigureAwait(false);

            return ListResponse<TEntity>.Create(listRequest, items);
        }
    }
}
