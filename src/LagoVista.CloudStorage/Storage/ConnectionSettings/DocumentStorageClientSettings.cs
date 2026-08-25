using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public enum DocumentStorageClientType
    {
        Cosmos,
        Mongo
    }

    public interface IDocumentStorageProviderSettings
    {
        DocumentStorageClientType Provider { get; }
    }

    public interface ICosmosConnectionSettings
    {
        string Endpoint { get; }
        string AccessKey { get; }
        string DatabaseName { get; }
    }

    public interface IMongoConnectionSettings
    {
        string ConnectionString { get; }
        string DatabaseName { get; }
    }

    public sealed class DocumentStorageProviderSettings : IDocumentStorageProviderSettings
    {
        public const string SectionName = "DefaultDocDBStorage";

        public DocumentStorageProviderSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            var section = configuration.GetSection(SectionName);
            var provider = section["Provider"];
            Provider = String.IsNullOrWhiteSpace(provider) || provider.Equals("Cosmos", StringComparison.OrdinalIgnoreCase) || provider.Equals("CosmosDB", StringComparison.OrdinalIgnoreCase)
                ? DocumentStorageClientType.Cosmos
                : provider.Equals("Mongo", StringComparison.OrdinalIgnoreCase) || provider.Equals("MongoDB", StringComparison.OrdinalIgnoreCase)
                    ? DocumentStorageClientType.Mongo
                    : throw new InvalidOperationException($"Unknown document storage provider '{provider}'. Expected Cosmos or Mongo.");
        }

        public DocumentStorageClientType Provider { get; }
    }

    public sealed class CosmosConnectionSettings : ICosmosConnectionSettings
    {
        public const string SectionName = "DefaultDocDBStorage";

        public CosmosConnectionSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            var section = configuration.GetSection(SectionName);
            Endpoint = section.Require("Endpoint");
            AccessKey = section.Require("AccessKey");
            DatabaseName = section.Require("DbName");
        }

        public string Endpoint { get; }
        public string AccessKey { get; }
        public string DatabaseName { get; }
    }

    public sealed class MongoConnectionSettings : IMongoConnectionSettings
    {
        public const string SectionName = "MongoDocumentStorage";

        public MongoConnectionSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            var mongo = new MongoDocumentStorageConnectionSettings(configuration);
            ConnectionString = mongo.BuildConnectionString();

            var section = configuration.GetSection(SectionName);
            DatabaseName = section.Require("DatabaseName");
        }

        public string ConnectionString { get; }
        public string DatabaseName { get; }
    }
}
