using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    public class ApplicationStorageSmokeTest : IPlatformSmokeTest
    {
        private readonly IDocumentStorageProviderSettings _providerSettings;
        private readonly IMongoDocumentStorageConnectionSettings _settings;

        public ApplicationStorageSmokeTest(IDocumentStorageProviderSettings providerSettings, IMongoDocumentStorageConnectionSettings settings)
        {
            _providerSettings = providerSettings ?? throw new ArgumentNullException(nameof(providerSettings));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string Key => "storage.mongo.application";
        public string Name => "Application Data Storage (Mongo)";
        public string Category => "Data Storage";

        public async Task<PlatformSmokeTestResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var conectionString = _settings.BuildConnectionString();

            var client = new MongoClient(conectionString);
            var database = client.GetDatabase(_settings.DatabaseName);
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken).ConfigureAwait(false);

            return new PlatformSmokeTestResult
            {
                Status = PlatformSmokeTestStatus.Passed,
                Target = GetSanitizedTarget(conectionString, _settings.DatabaseName),
                Message = "Application Data (Mongo) ping succeeded."
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
