using LagoVista.CloudStorage.DocumentDB;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public interface IDocumentStorageProviderSettings
    {
        DocumentStorageProviderType Provider { get; }
    }

    public sealed class DocumentStorageProviderSettings : IDocumentStorageProviderSettings
    {
        public const string SectionName = "DefaultDocDBStorage";

        public DocumentStorageProviderSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var provider = configuration.GetSection(SectionName)["Provider"];
            Provider = DocumentStorageSettingsResolver.ParseProvider(provider);
        }

        public DocumentStorageProviderType Provider { get; }
    }
}
