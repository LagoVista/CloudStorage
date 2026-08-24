using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public interface IDocumentStorageSettingsProvider
    {
        DocumentStorageSettings Default { get; }
    }

    /// <summary>
    /// Creates the default document-storage adapter settings from the normal configuration
    /// sections. Cosmos remains the default provider. Mongo connection settings are only read
    /// when Mongo is selected so Cosmos-only deployments do not need Mongo credentials.
    /// </summary>
    public sealed class DocumentStorageSettingsProvider : IDocumentStorageSettingsProvider
    {
        public const string SectionName = "DefaultDocDBStorage";

        public DocumentStorageSettingsProvider(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);
            var provider = DocumentStorageSettingsResolver.ParseProvider(section["Provider"]);
            var logicalDatabaseName = section.Require("DbName");

            if (provider == DocumentStorageProviderType.Cosmos)
            {
                Default = new DocumentStorageSettings
                {
                    Provider = DocumentStorageProviderType.Cosmos,
                    Endpoint = section.Require("Endpoint"),
                    SharedKey = section.Require("AccessKey"),
                    DatabaseName = logicalDatabaseName
                };
                return;
            }

            var mongoConnection = new MongoDocumentStorageConnectionSettings(configuration);
            var mongoDatabaseName = String.IsNullOrWhiteSpace(section["MongoDbName"])
                ? logicalDatabaseName
                : section["MongoDbName"].Trim();

            Default = new DocumentStorageSettings
            {
                Provider = DocumentStorageProviderType.Mongo,
                Endpoint = section["Endpoint"],
                SharedKey = section["AccessKey"],
                DatabaseName = logicalDatabaseName,
                Mongo = new MongoDocumentStorageSettings
                {
                    ConnectionString = mongoConnection.BuildConnectionString(),
                    DatabaseName = mongoDatabaseName
                }
            };
        }

        public DocumentStorageSettings Default { get; }
    }
}
