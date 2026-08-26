using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentStorageClient
    {
        Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<OperationResponse<TEntity>> UpsertDocumentAsync<TEntity>(TEntity item, string eTag = null)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<SyncUpsertResult> UpsertDocumentAsync(JObject document, string expectedETag = null, CancellationToken cancellationToken = default);

        Task<TEntity> GetDocumentAsync<TEntity>(string id, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<TEntity> GetDocumentAsync<TEntity>(string id, string partitionKey, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<TProjection> GetDocumentProjectionAsync<TProjection>(string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class;

        Task<TProjection> GetDocumentProjectionAsync<TProjection>(string entityType, string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class;

        Task<TProjection> GetOwnedDocumentProjectionAsync<TProjection>(string id, string ownerOrganizationId, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class;

        Task<TProjection> GetDocumentProjectionByKeyAsync<TProjection>(string entityType, string key, string ownerOrganizationId, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class;

        Task<IEnumerable<TProjection>> GetDocumentProjectionsAsync<TProjection>(string entityType, Expression<Func<TProjection, bool>> query, CancellationToken cancellationToken = default)
            where TProjection : class;

        Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id, string partitionKey)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<OperationResponse<TEntity>> PatchDocumentAsync<TEntity>(PatchRequest request)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest, bool descending)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<ListResponse<TEntity>> QueryAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> sort, ListRequest listRequest, bool descending)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<ListResponse<TEntity>> QueryAllAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity;

        Task<ListResponse<TEntityFactory>> QuerySummaryAsync<TEntityFactory>(string entityType, Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest, bool descending)
            where TEntityFactory : class, ISummaryFactory, INoSQLEntity, ICategorized, IAuditableEntity;

        Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(string entityType, DocumentQueryRequest request, CancellationToken cancellationToken = default)
            where TResult : class;
    }

    public interface ICosmosDocumentStorageClient : IDocumentStorageClient
    {
    }

    public interface IMongoDocumentStorageClient : IDocumentStorageClient
    {
    }

    public interface IDocumentStorageClientProvider
    {
        IDocumentStorageClient GetClient();
    }
}