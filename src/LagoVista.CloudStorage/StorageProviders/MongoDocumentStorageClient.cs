using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class MongoDocumentStorageClient : IMongoDocumentStorageClient
    {
        private readonly DocumentStorageSettings _settings;
        private readonly IAdminLogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IDependencyManager _dependencyManager;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;
        private readonly IDocumentCollectionFactory _collectionFactory;

        public MongoDocumentStorageClient(
            IDocumentStorageClientSettings settings,
            IAdminLogger logger,
            ICacheProvider cacheProvider,
            IDependencyManager dependencyManager,
            IDocumentCollectionNameResolver collectionNameResolver,
            IDocumentCollectionFactory collectionFactory)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider;
            _dependencyManager = dependencyManager;
            _collectionNameResolver = collectionNameResolver ?? throw new ArgumentNullException(nameof(collectionNameResolver));
            _collectionFactory = collectionFactory ?? throw new ArgumentNullException(nameof(collectionFactory));

            _settings = new DocumentStorageSettings
            {
                Provider = DocumentStorageProviderType.Mongo,
                DatabaseName = settings.DatabaseName,
                Mongo = new MongoDocumentStorageSettings
                {
                    ConnectionString = settings.MongoConnectionString,
                    DatabaseName = settings.MongoDatabaseName
                }
            };
        }

        public Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().CreateDocumentAsync(item);

        public Task<OperationResponse<TEntity>> UpsertDocumentAsync<TEntity>(TEntity item)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().UpsertDocumentAsync(item);

        public Task<TEntity> GetDocumentAsync<TEntity>(string id, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().GetDocumentAsync(id, throwOnNotFound);

        public Task<TEntity> GetDocumentAsync<TEntity>(string id, string partitionKey, bool throwOnNotFound = true)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().GetDocumentAsync(id, partitionKey, throwOnNotFound);

        public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().DeleteDocumentAsync(id);

        public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id, string partitionKey)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().DeleteDocumentAsync(id, partitionKey);

        public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().QueryAsync(query);

        public Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest)
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            GetStorage<TEntity>().QueryAsync(query, listRequest);

        public Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(string entityType, DocumentQueryRequest request, CancellationToken cancellationToken = default)
            where TResult : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required for Mongo known-query routing.", nameof(entityType));
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (!_collectionNameResolver.TryResolve(_settings.Mongo.DatabaseName, entityType, out var collectionName))
                throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            return _collectionFactory.Create(_settings, collectionName).QueryAsync<TResult>(request, cancellationToken);
        }

        private IDocumentDBRepoBase<TEntity> GetStorage<TEntity>()
            where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity =>
            DocumentStorageFactory.Create<TEntity>(_settings, _logger, _cacheProvider, _dependencyManager, collectionNameResolver: _collectionNameResolver);
    }
}
