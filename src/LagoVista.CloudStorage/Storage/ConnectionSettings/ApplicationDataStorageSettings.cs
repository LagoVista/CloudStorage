using Microsoft.Extensions.Configuration;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public interface IApplicationDataStorageSettings : IMongoDocumentStorageConnectionSettings
    {
    }

    public sealed class ApplicationDataStorageSettings : MongoDocumentStorageConnectionSettings, IApplicationDataStorageSettings
    {
        public const string SectionName = "ApplicationDataStorage";

        public ApplicationDataStorageSettings(IConfiguration configuration)
            : base(configuration, SectionName)
        {
        }
    }
}
