using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.StorageProviders;
using System;

namespace LagoVista.CloudStorage.DocumentDB
{
    public sealed class DocumentCollectionFactory : IDocumentCollectionFactory
    {
        private readonly ICosmosClientProvider _cosmosClientProvider;

        public DocumentCollectionFactory(ICosmosClientProvider cosmosClientProvider)
        {
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
        }

        public IDocumentCollection Create(string endpoint, string sharedKey, string databaseName, string collectionName = null)
        {
            return Create(DocumentStorageSettingsResolver.Resolve(endpoint, sharedKey, databaseName), collectionName);
        }

        public IDocumentCollection Create(DocumentStorageSettings settings, string collectionName = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var resolvedCollectionName = String.IsNullOrWhiteSpace(collectionName) ? $"{settings.DatabaseName}_Collections" : collectionName;
            switch (settings.Provider)
            {
                case DocumentStorageProviderType.Cosmos:
                    return new CosmosDocumentCollection(_cosmosClientProvider, settings.Endpoint, settings.SharedKey, settings.DatabaseName, resolvedCollectionName);

                case DocumentStorageProviderType.Mongo:
                    if (settings.Mongo == null) throw new InvalidOperationException("Mongo document storage settings are required when Mongo is selected.");
                    return new MongoDocumentCollection(settings.Mongo.ConnectionString, settings.Mongo.DatabaseName, resolvedCollectionName);

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{settings.Provider}'.");
            }
        }
    }
}
