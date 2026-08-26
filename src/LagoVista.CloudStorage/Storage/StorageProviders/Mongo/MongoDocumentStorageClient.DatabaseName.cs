namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class MongoDocumentStorageClient
    {
        public string DatabaseName => _settings.DatabaseName;
    }
}
