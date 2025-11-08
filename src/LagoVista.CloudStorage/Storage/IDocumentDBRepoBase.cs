// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: c794efed7309a7be52ce1cc899a452ae897da3d5310b4c5c0d7fcc6b51a96e38
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.DocumentDB
{
    public interface IDocumentDBRepoBase<TEntity> where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
    {
        Task DeleteCollectionAsync();
        string GetCollectionName();
        string GetPartitionKey();
        void SetConnection(string connectionString, string sharedKey, string dbName);
       
        Task<OperationResponse<TEntity>> CreateDocumentAsync(TEntity item);
        Task<OperationResponse<TEntity>> UpsertDocumentAsync(TEntity item);
        Task<TEntity> GetDocumentAsync(string id, bool throwOnNotFound = true);
        Task<TEntity> GetDocumentAsync(string id, string partitionKey, bool throwOnNotFound = true);
        Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id);
        Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id, string partitionKey);
        Task<IEnumerable<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query);
        Task<IEnumerable<TEntity>> QueryAsync(string sql, params QueryParameter[] sqlParams);
        Task<ListResponse<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, ListRequest listRequest);
        Task<ListResponse<TEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query,
                                System.Linq.Expressions.Expression<Func<TEntity, string>> sort, ListRequest listRequest);
        Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary, TEntityFactory>(System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> query,
                              System.Linq.Expressions.Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest)
            where TEntitySummary : class, ISummaryData where TEntityFactory : class, ISummaryFactory, INoSQLEntity;
        Task<ListResponse<TEntitySummary>> QuerySummaryDescendingAsync<TEntitySummary, TEntityFactory>(System.Linq.Expressions.Expression<Func<TEntityFactory, bool>> query,
                   System.Linq.Expressions.Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest) where TEntitySummary : class, ISummaryData where TEntityFactory :
            class, ISummaryFactory, INoSQLEntity;
        Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TEntitySummary : class;
        Task<ListResponse<TEntity>> QueryDescendingAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query,
                      System.Linq.Expressions.Expression<Func<TEntity, string>> sort, ListRequest listRequest);
        Task<ListResponse<TEntity>> QueryAllAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, ListRequest listRequest);
        Task<ListResponse<TEntity>> DescOrderQueryAsync<TKey>(System.Linq.Expressions.Expression<Func<TEntity, bool>> query,
                                                    System.Linq.Expressions.Expression<Func<TEntity, TKey>> orderBy,
                                                    ListRequest listRequest);

        Task<ListResponse<TMiscEntity>> QueryAsync<TMiscEntity>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TMiscEntity : class;
    }
}