namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class CosmosDocumentStorageClient
    {
        public string DatabaseName => _settings.DatabaseName;
    }
}
