using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    public class MongoDocumentStorageSmokeTest : IPlatformSmokeTest
    {
        private readonly IDocumentStorageProviderSettings _providerSettings;
        private readonly IMongoDocumentStorageConnectionSettings _settings;
        private readonly ISyncConnectionSettings _syncSettings;

        public MongoDocumentStorageSmokeTest(
            IDocumentStorageProviderSettings providerSettings,
            IMongoDocumentStorageConnectionSettings settings,
            ISyncConnectionSettings syncSettings)
        {
            _providerSettings = providerSettings ?? throw new ArgumentNullException(nameof(providerSettings));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _syncSettings = syncSettings ?? throw new ArgumentNullException(nameof(syncSettings));
        }

        public string Key => "storage.mongo.documents";
        public string Name => "Mongo Document Storage";
        public string Category => "Data Storage";

        public async Task<PlatformSmokeTestResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_providerSettings.Provider != DocumentStorageProviderType.Mongo)
            {
                return new PlatformSmokeTestResult
                {
                    Status = PlatformSmokeTestStatus.Skipped,
                    Message = $"Document storage provider is {_providerSettings.Provider}."
                };
            }

            var connectionString = _settings.BuildConnectionString();
            var databaseName = _syncSettings.SyncConnectionSettings.ResourceName;
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken).ConfigureAwait(false);

            return new PlatformSmokeTestResult
            {
                Status = PlatformSmokeTestStatus.Passed,
                Target = GetSanitizedTarget(connectionString, databaseName),
                Message = "Mongo ping succeeded."
            };
        }

        private static string GetSanitizedTarget(string connectionString, string databaseName)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) return databaseName;

            var schemeEnd = connectionString.IndexOf("://", StringComparison.Ordinal);
            var remainder = schemeEnd >= 0 ? connectionString.Substring(schemeEnd + 3) : connectionString;
            var at = remainder.LastIndexOf('@');
            if (at >= 0) remainder = remainder.Substring(at + 1);

            var end = remainder.IndexOfAny(new[] { '/', '?' });
            var host = end >= 0 ? remainder.Substring(0, end) : remainder;
            return String.IsNullOrWhiteSpace(databaseName) ? host : $"{host}/{databaseName}";
        }
    }
}
