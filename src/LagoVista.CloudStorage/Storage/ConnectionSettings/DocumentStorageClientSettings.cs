using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Storage
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
}

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
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
}
