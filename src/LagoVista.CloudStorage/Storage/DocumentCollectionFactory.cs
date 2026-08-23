using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.StorageProviders;
using System;

namespace LagoVista.CloudStorage.DocumentDB
{
    public sealed class DocumentCollectionFactory : IDocumentCollectionFactory
    {
        private readonly ICosmosClientProvider _cosmosClientProvider;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;

        public DocumentCollectionFactory(ICosmosClientProvider cosmosClientProvider, IDocumentCollectionNameResolver collectionNameResolver = null)
        {
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            _collectionNameResolver = collectionNameResolver ?? new DocumentCollectionNameResolver();
        }

        public IDocumentCollection Create(string endpoint, string sharedKey, string databaseName, string collectionName = null)
        {
            return Create(DocumentStorageSettingsResolver.Resolve(endpoint, sharedKey, databaseName), collectionName);
        }

        public IDocumentCollection Create(DocumentStorageSettings settings, string collectionName = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var resolvedCollectionName = String.IsNullOrWhiteSpace(collectionName) ? $"{settings.DatabaseName}_Collections" : collectionName;
            return CreateCore(settings, resolvedCollectionName);
        }

        public IDocumentCollection Create<TEntity>(string endpoint, string sharedKey, string databaseName, string collectionName = null) where TEntity : class
        {
            return Create<TEntity>(DocumentStorageSettingsResolver.Resolve(endpoint, sharedKey, databaseName), collectionName);
        }

        public IDocumentCollection Create<TEntity>(DocumentStorageSettings settings, string collectionName = null) where TEntity : class
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings.Provider == DocumentStorageProviderType.Cosmos)
            {
                var cosmosCollectionName = String.IsNullOrWhiteSpace(collectionName) ? $"{settings.DatabaseName}_Collections" : collectionName;
                return CreateCore(settings, cosmosCollectionName);
            }

            var mongoDatabaseName = settings.Mongo?.DatabaseName ?? settings.DatabaseName;
            var mongoCollectionName = _collectionNameResolver.Resolve(mongoDatabaseName, typeof(TEntity), collectionName);
            return CreateCore(settings, mongoCollectionName);
        }

        private IDocumentCollection CreateCore(DocumentStorageSettings settings, string collectionName)
        {
            switch (settings.Provider)
            {
                case DocumentStorageProviderType.Cosmos:
                    return new CosmosDocumentCollection(_cosmosClientProvider, settings.Endpoint, settings.SharedKey, settings.DatabaseName, collectionName);

                case DocumentStorageProviderType.Mongo:
                    if (settings.Mongo == null) throw new InvalidOperationException("Mongo document storage settings are required when Mongo is selected.");
                    return new MongoDocumentCollection(settings.Mongo.ConnectionString, settings.Mongo.DatabaseName, collectionName);

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{settings.Provider}'.");
            }
        }
    }
}
