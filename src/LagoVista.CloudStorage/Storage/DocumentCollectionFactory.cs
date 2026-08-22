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
            var settings = DocumentStorageSettingsResolver.Resolve(endpoint, sharedKey, databaseName);
            var resolvedCollectionName = String.IsNullOrWhiteSpace(collectionName) ? $"{databaseName}_Collections" : collectionName;

            switch (settings.Provider)
            {
                case DocumentStorageProviderType.Cosmos:
                    return new CosmosDocumentCollection(_cosmosClientProvider, settings.Endpoint, settings.SharedKey, settings.DatabaseName, resolvedCollectionName);

                case DocumentStorageProviderType.Mongo:
                    throw new NotSupportedException("Mongo document collection storage is selected but the Mongo provider is not implemented yet.");

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{settings.Provider}'.");
            }
        }
    }
}
