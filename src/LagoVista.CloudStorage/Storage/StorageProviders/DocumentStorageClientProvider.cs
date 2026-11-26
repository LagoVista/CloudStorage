using LagoVista.CloudStorage.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LagoVista.CloudStorage.Storage.StorageProviders
{
    /// <summary>
    /// Resolves only the configured document provider. The inactive provider is not
    /// constructed, so a Mongo deployment does not require Cosmos credentials (or
    /// vice versa) merely to build the document storage service graph.
    /// </summary>
    public sealed class DocumentStorageClientProvider : IDocumentStorageClientProvider
    {
        private readonly IDocumentStorageProviderSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        public DocumentStorageClientProvider(IDocumentStorageProviderSettings settings, IServiceProvider serviceProvider)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IDocumentStorageClient GetClient()
        {
            switch (_settings.Provider)
            {
                case DocumentStorageClientType.Cosmos:
                    return _serviceProvider.GetRequiredService<ICosmosDocumentStorageClient>();

                case DocumentStorageClientType.Mongo:
                    return _serviceProvider.GetRequiredService<IMongoDocumentStorageClient>();

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{_settings.Provider}'.");
            }
        }
    }
}
