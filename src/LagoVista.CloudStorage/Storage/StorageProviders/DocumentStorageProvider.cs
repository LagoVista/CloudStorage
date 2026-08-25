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
        public MongoDocumentStorageSettings Mongo { get; set; }
    }

    /// <summary>
    /// Resolves provider selection independently from provider credentials. Cosmos remains the
    /// default and retains its existing constructor settings. Mongo uses explicit configuration
    /// so both providers can be configured in the same process for migration and cutover.
    /// </summary>
    public static class DocumentStorageSettingsResolver
    {
        public const string ProviderEnvironmentVariable = "NUVIOT_DOCUMENT_STORAGE_PROVIDER";
        public const string ProviderEnvironmentVariablePrefix = "NUVIOT_DOCUMENT_STORAGE_PROVIDER_";
        public const string MongoConnectionStringEnvironmentVariable = "NUVIOT_MONGO_CONNECTION_STRING";
        public const string MongoConnectionStringEnvironmentVariablePrefix = "NUVIOT_MONGO_CONNECTION_STRING_";
        public const string MongoDatabaseEnvironmentVariable = "NUVIOT_MONGO_DATABASE";
        public const string MongoDatabaseEnvironmentVariablePrefix = "NUVIOT_MONGO_DATABASE_";

        public static DocumentStorageSettings Resolve(string endpoint, string sharedKey, string databaseName)
        {
            var providerSetting = GetProviderSetting(databaseName);
            return Resolve(endpoint, sharedKey, databaseName, providerSetting);
        }

        public static DocumentStorageSettings Resolve(string endpoint, string sharedKey, string databaseName, string providerSetting)
        {
            return Resolve(endpoint, sharedKey, databaseName, providerSetting, null);
        }

        public static DocumentStorageSettings Resolve(string endpoint, string sharedKey, string databaseName, string providerSetting, MongoDocumentStorageSettings mongoSettings)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));

            var provider = ParseProvider(providerSetting);
            if (provider == DocumentStorageProviderType.Cosmos && String.IsNullOrWhiteSpace(endpoint)) throw new ArgumentNullException(nameof(endpoint));

            return new DocumentStorageSettings()
            {
                Provider = provider,
                Endpoint = endpoint,
                SharedKey = sharedKey,
                DatabaseName = databaseName,
                Mongo = provider == DocumentStorageProviderType.Mongo ? ValidateMongoSettings(mongoSettings ?? ResolveMongo(databaseName)) : mongoSettings
            };
        }

        public static MongoDocumentStorageSettings ResolveMongo(string logicalDatabaseName)
        {
            if (String.IsNullOrWhiteSpace(logicalDatabaseName)) throw new ArgumentNullException(nameof(logicalDatabaseName));

            var normalizedDatabaseName = NormalizeEnvironmentKey(logicalDatabaseName);
            var connectionString = GetEnvironmentSetting(MongoConnectionStringEnvironmentVariablePrefix + normalizedDatabaseName, MongoConnectionStringEnvironmentVariable);
            var databaseName = GetEnvironmentSetting(MongoDatabaseEnvironmentVariablePrefix + normalizedDatabaseName, MongoDatabaseEnvironmentVariable);

            if (String.IsNullOrWhiteSpace(connectionString))
                connectionString = Utils.TestConnections.DevMongoDocumentStorage.BuildConnectionString();

            if (String.IsNullOrWhiteSpace(databaseName))
                databaseName = logicalDatabaseName;

            return ValidateMongoSettings(new MongoDocumentStorageSettings
            {
                ConnectionString = connectionString,
                DatabaseName = databaseName
            });
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

        private static MongoDocumentStorageSettings ValidateMongoSettings(MongoDocumentStorageSettings settings)
        {
            if (settings == null) throw new InvalidOperationException("Mongo document storage settings are required when Mongo is selected.");
            if (String.IsNullOrWhiteSpace(settings.ConnectionString)) throw new InvalidOperationException($"Mongo document storage connection string is required. Configure {MongoConnectionStringEnvironmentVariable} or its database-specific override.");
            if (!settings.ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) && !settings.ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Mongo document storage connection string must use mongodb:// or mongodb+srv://.");
            if (String.IsNullOrWhiteSpace(settings.DatabaseName)) throw new InvalidOperationException("Mongo document storage database name is required.");
            return settings;
        }

        private static string GetProviderSetting(string databaseName)
        {
            return GetEnvironmentSetting(ProviderEnvironmentVariablePrefix + NormalizeEnvironmentKey(databaseName), ProviderEnvironmentVariable);
        }

        private static string GetEnvironmentSetting(string specificVariableName, string globalVariableName)
        {
            var databaseSpecificSetting = Environment.GetEnvironmentVariable(specificVariableName);
            if (!String.IsNullOrWhiteSpace(databaseSpecificSetting)) return databaseSpecificSetting;
            return Environment.GetEnvironmentVariable(globalVariableName);
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

    public static class DocumentStorageFactory
    {
        public static IDocumentDBRepoBase<TEntity> ResolveAndCreate<TEntity>(string endpoint, string sharedKey, string databaseName, IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null, ICosmosClientProvider cosmosClientProvider = null, IDocumentCollectionNameResolver collectionNameResolver = null) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            var settings = DocumentStorageSettingsResolver.Resolve(endpoint, sharedKey, databaseName);
            return Create<TEntity>(settings, logger, cacheProvider, dependencyManager, cosmosClientProvider, collectionNameResolver);
        }

        public static IDocumentDBRepoBase<TEntity> Create<TEntity>(DocumentStorageSettings settings, IAdminLogger logger, ICacheProvider cacheProvider = null, IDependencyManager dependencyManager = null, ICosmosClientProvider cosmosClientProvider = null, IDocumentCollectionNameResolver collectionNameResolver = null) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            switch (settings.Provider)
            {
                case DocumentStorageProviderType.Cosmos:
                    return new CosmosDBStorage<TEntity>(settings.Endpoint, settings.SharedKey, settings.DatabaseName, logger, cacheProvider, dependencyManager, cosmosClientProvider);

                case DocumentStorageProviderType.Mongo:
                    if (settings.Mongo == null) throw new InvalidOperationException("Mongo settings are required when the Mongo document storage provider is selected.");
                    MongoBsonSerialization.Configure();
                    return new MongoDBStorage<TEntity>(settings.Mongo.ConnectionString, settings.Mongo.DatabaseName, logger, cacheProvider, dependencyManager, collectionNameResolver);

                default:
                    throw new InvalidOperationException($"Unsupported document storage provider '{settings.Provider}'.");
            }
        }
    }
}
