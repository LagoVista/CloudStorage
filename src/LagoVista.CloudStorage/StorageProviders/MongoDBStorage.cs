using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.Azure.Cosmos;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    internal class MongoDBStorage<TEntity> : IDocumentDBRepoBase<TEntity> where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
    {
        MongoClient _mongoClient;
        IMongoDatabase _mongoDb;
        IMongoCollection<TEntity> _mongoCollection;

        public Task<OperationResponse<TEntity>> CreateDocumentAsync(TEntity item)
        {
            throw new NotImplementedException();
        }

        public Task DeleteCollectionAsync()
        {
            throw new NotImplementedException();
        }

        public Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResponse<TEntity>> DeleteDocumentAsync(string id, string partitionKey)
        {
            throw new NotImplementedException();
        }

        public Task<ListResponse<TEntity>> DescOrderQueryAsync<TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> orderBy, ListRequest listRequest)
        {
            throw new NotImplementedException();
        }

        public string GetCollectionName()
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> GetDocumentAsync(string id, bool throwOnNotFound = true)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> GetDocumentAsync(string id, string partitionKey, bool throwOnNotFound = true)
        {
            throw new NotImplementedException();
        }

        public string GetPartitionKey()
        {
            throw new NotImplementedException();
        }

        public Task<ListResponse<TEntity>> QueryAllAsync(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> query)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TEntity>> QueryAsync(string sql, params QueryParameter[] sqlParams)
        {
            throw new NotImplementedException();
        }

        public Task<ListResponse<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ListResponse<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ListResponse<TMiscEntity>> QueryAsync<TMiscEntity>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TMiscEntity : class
        {
            throw new NotImplementedException();
        }

        public Task<ListResponse<TEntity>> QueryDescendingAsync(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ListResponse<TEntitySummary>> QuerySummaryAsync<TEntitySummary>(string sql, ListRequest listRequest, params QueryParameter[] sqlParams) where TEntitySummary : class
        {
            throw new NotImplementedException();
        }

        public void SetConnection(string connectionString, string sharedKey, string dbName)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResponse<TEntity>> UpsertDocumentAsync(TEntity item)
        {
            throw new NotImplementedException();
        }

        Task<ListResponse<TEntitySummary>> IDocumentDBRepoBase<TEntity>.QuerySummaryAsync<TEntitySummary, TEntityFactory>(Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest)
        {
            throw new NotImplementedException();
        }

        Task<ListResponse<TEntitySummary>> IDocumentDBRepoBase<TEntity>.QuerySummaryDescendingAsync<TEntitySummary, TEntityFactory>(Expression<Func<TEntityFactory, bool>> query, Expression<Func<TEntityFactory, string>> sort, ListRequest listRequest)
        {
            throw new NotImplementedException();
        }
    }
}
