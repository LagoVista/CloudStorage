using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public interface IApplicationDataStorageSettings : IMongoDocumentStorageConnectionSettings
    {
    }

    public sealed class ApplicationDataStorageSettings : MongoDocumentStorageConnectionSettings, IApplicationDataStorageSettings
    {
        public const string SectionName = "ApplicationDataStorage";

        public ApplicationDataStorageSettings(IConfiguration configuration)
            : base(configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            DatabaseName = configuration.GetSection(SectionName).Require("DatabaseName");
        }
    }
}
