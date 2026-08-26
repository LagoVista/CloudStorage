using LagoVista.CloudStorage.Models;
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
        public async Task<ListResponse<TEntity>> QueryAllAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            var items = await GetCollection<TEntity>()
                .Find(query)
                .Skip(Math.Max(0, listRequest.PageIndex - 1) * listRequest.PageSize)
                .Limit(listRequest.PageSize)
                .ToListAsync()
                .ConfigureAwait(false);

            return ListResponse<TEntity>.Create(listRequest, items);
        }
    }
}
