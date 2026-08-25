using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    public class CosmosDocumentStorageSmokeTest : IPlatformSmokeTest
    {
        private readonly IDocumentStorageProviderSettings _providerSettings;
        private readonly ICosmosConnectionSettings _settings;
        private readonly ICosmosClientProvider _clientProvider;

        public CosmosDocumentStorageSmokeTest(IDocumentStorageProviderSettings providerSettings, ICosmosConnectionSettings settings, ICosmosClientProvider clientProvider)
        {
            _providerSettings = providerSettings ?? throw new ArgumentNullException(nameof(providerSettings));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
        }

        public string Key => "storage.cosmos.documents";
        public string Name => "Cosmos Document Storage";
        public string Category => "Data Storage";

        public async Task<PlatformSmokeTestResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_providerSettings.Provider != DocumentStorageClientType.Cosmos)
            {
                return new PlatformSmokeTestResult
                {
                    Status = PlatformSmokeTestStatus.Skipped,
                    Message = $"Document storage provider is {_providerSettings.Provider}."
                };
            }

            var client = _clientProvider.GetClient(_settings.Endpoint, _settings.AccessKey);
            var database = client.GetDatabase(_settings.DatabaseName);
            await database.ReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            return new PlatformSmokeTestResult
            {
                Status = PlatformSmokeTestStatus.Passed,
                Target = GetSanitizedTarget(_settings.Endpoint, _settings.DatabaseName),
                Message = "Cosmos database read succeeded."
            };
        }

        private static string GetSanitizedTarget(string endpoint, string databaseName)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return databaseName;
            return String.IsNullOrWhiteSpace(databaseName) ? uri.Authority : $"{uri.Authority}/{databaseName}";
        }
    }
}
