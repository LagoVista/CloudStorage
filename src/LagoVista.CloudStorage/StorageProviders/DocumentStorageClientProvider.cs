using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using System;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed class DocumentStorageClientProvider : IDocumentStorageClientProvider
    {
        private readonly IDocumentStorageClientSettings _settings;
        private readonly ICosmosDocumentStorageClient _cosmosClient;
        private readonly IMongoDocumentStorageClient _mongoClient;

        public DocumentStorageClientProvider(
            IDocumentStorageClientSettings settings,
            ICosmosDocumentStorageClient cosmosClient,
            IMongoDocumentStorageClient mongoClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
        }

        public IDocumentStorageClient GetClient()
        {
            switch (_settings.Provider)
            {
                case DocumentStorageProviderType.Cosmos:
                    return _cosmosClient;

                case DocumentStorageProviderType.Mongo:
                    return _mongoClient;

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{_settings.Provider}'.");
            }
        }
    }
}
