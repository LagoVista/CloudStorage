using LagoVista.Core;

namespace LagoVista.CloudStorage.StorageProviders
{
    [CriticalCoverage]
    public sealed partial class MongoDocumentStorageClient
    {
        public string DatabaseName => _settings.DatabaseName;
    }
}
