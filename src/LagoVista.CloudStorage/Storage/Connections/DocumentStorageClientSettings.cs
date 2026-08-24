using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public interface IDocumentStorageClientSettings
    {
        DocumentStorageProviderType Provider { get; }
        string Endpoint { get; }
        string AccessKey { get; }
        string DatabaseName { get; }
        string MongoConnectionString { get; }
        string MongoDatabaseName { get; }
    }

    public sealed class DocumentStorageClientSettings : IDocumentStorageClientSettings
    {
        public const string SectionName = "DefaultDocDBStorage";

        public DocumentStorageClientSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);
            DatabaseName = section.Require("DbName");
            Provider = DocumentStorageSettingsResolver.ParseProvider(section["Provider"]);

            if (Provider == DocumentStorageProviderType.Cosmos)
            {
                Endpoint = section.Require("Endpoint");
                AccessKey = section.Require("AccessKey");
                return;
            }

            var mongo = new MongoDocumentStorageConnectionSettings(configuration);
            MongoConnectionString = mongo.BuildConnectionString();
            MongoDatabaseName = String.IsNullOrWhiteSpace(section["MongoDbName"])
                ? DatabaseName
                : section["MongoDbName"].Trim();
        }

        public DocumentStorageProviderType Provider { get; }
        public string Endpoint { get; }
        public string AccessKey { get; }
        public string DatabaseName { get; }
        public string MongoConnectionString { get; }
        public string MongoDatabaseName { get; }

        public override string ToString()
        {
            return $"DocumentStorageClientSettings(Provider={Provider}, DatabaseName={DatabaseName}, MongoDatabaseName={MongoDatabaseName ?? "<none>"}, Endpoint=<redacted>, AccessKey=<redacted>, MongoConnectionString=<redacted>)";
        }
    }
}
