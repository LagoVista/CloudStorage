using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Text;

namespace LagoVista.CloudStorage.DocumentDB
{
    public enum DocumentStorageProviderType
    {
        Cosmos,
        Mongo
    }

    public sealed class DocumentStorageSettings
    {
        public DocumentStorageProviderType Provider { get; set; }
        public string Endpoint { get; set; }
        public string SharedKey { get; set; }
        public string DatabaseName { get; set; }
    }

    /// <summary>
    /// Resolves the logical document storage provider without requiring changes to the
    /// existing repository constructor signatures. Cosmos remains the default so adding
    /// this seam is behavior preserving for every existing deployment.
    /// </summary>
    public static class DocumentStorageSettingsResolver
    {
        public const string ProviderEnvironmentVariable = "NUVIOT_DOCUMENT_STORAGE_PROVIDER";
        public const string ProviderEnvironmentVariablePrefix = "NUVIOT_DOCUMENT_STORAGE_PROVIDER_";

        public static DocumentStorageSettings Resolve(string endpoint, string sharedKey, string databaseName)
        {
            var providerSetting = GetProviderSetting(databaseName);
            return Resolve(endpoint, sharedKey, databaseName, providerSetting);
        }

        public static DocumentStorageSettings Resolve(string endpoint, string sharedKey, string databaseName, string providerSetting)
        {
            if (String.IsNullOrWhiteSpace(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));

            var provider = ParseProvider(providerSetting);
            return new DocumentStorageSettings()
            {
                Provider = provider,
                Endpoint = endpoint,
                SharedKey = sharedKey,
                DatabaseName = databaseName
            };
        }

        public static DocumentStorageProviderType ParseProvider(string providerSetting)
        {
            if (String.IsNullOrWhiteSpace(providerSetting)) return DocumentStorageProviderType.Cosmos;

            switch (providerSetting.Trim().ToLowerInvariant())
            {
                case "cosmos":
                case "cosmosdb":
                case "azurecosmos":
                case "azurecosmosdb":
                    return DocumentStorageProviderType.Cosmos;

                case "mongo":
                case "mongodb":
                    return DocumentStorageProviderType.Mongo;

                default:
                    throw new InvalidOperationException($"Unknown document storage provider '{providerSetting}'. Expected Cosmos or Mongo.");
            }
        }

        private static string GetProviderSetting(string databaseName)
        {
            var databaseSpecificSetting = Environment.GetEnvironmentVariable(ProviderEnvironmentVariablePrefix + NormalizeEnvironmentKey(databaseName));
            if (!String.IsNullOrWhiteSpace(databaseSpecificSetting)) return databaseSpecificSetting;
            return Environment.GetEnvironmentVariable(ProviderEnvironmentVariable);
        }

        private static string NormalizeEnvironmentKey(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return String.Empty;

            var result = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (Char.IsLetterOrDigit(ch)) result.Append(Char.ToUpperInvariant(ch));
                else result.Append('_');
            }

            return result.ToString();
        }
    }

    /// <summary>
    /// Construction point for the rich entity document repository implementation.
    /// IDocumentCollection already supports Cosmos and Mongo; this factory remains Cosmos-only
    /// until MongoDBStorage implements the full entity workflow contract used by DocumentDBRepoBase.
    /// </summary>
    public static class DocumentStorageFactory
    {
        public static IDocumentDBRepoBase<TEntity> Create<TEntity>(DocumentStorageSettings settings, IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null, ICosmosClientProvider cosmosClientProvider = null) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            switch (settings.Provider)
            {
                case DocumentStorageProviderType.Cosmos:
                    return new CosmosDBStorage<TEntity>(settings.Endpoint, settings.SharedKey, settings.DatabaseName, logger, cacheProvider, dependencyManager, cosmosClientProvider);

                case DocumentStorageProviderType.Mongo:
                    throw new NotSupportedException("Mongo IDocumentCollection support is available, but the rich IDocumentDBRepoBase provider used by DocumentDBRepoBase is not implemented yet.");

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{settings.Provider}'.");
            }
        }
    }
}
