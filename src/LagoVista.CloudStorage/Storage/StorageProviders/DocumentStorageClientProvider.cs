using LagoVista.CloudStorage.Interfaces;
using System;

namespace LagoVista.CloudStorage.Storage.StorageProviders
{
    public sealed class DocumentStorageClientProvider : IDocumentStorageClientProvider
    {
        private readonly IDocumentStorageProviderSettings _settings;
        private readonly ICosmosDocumentStorageClient _cosmosClient;
        private readonly IMongoDocumentStorageClient _mongoClient;

        public DocumentStorageClientProvider(IDocumentStorageProviderSettings settings, ICosmosDocumentStorageClient cosmosClient, IMongoDocumentStorageClient mongoClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
        }

        public IDocumentStorageClient GetClient()
        {
            switch (_settings.Provider)
            {
                case DocumentStorageClientType.Cosmos:
                    return _cosmosClient;

                case DocumentStorageClientType.Mongo:
                    return _mongoClient;

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{_settings.Provider}'.");
            }
        }
    }
}
