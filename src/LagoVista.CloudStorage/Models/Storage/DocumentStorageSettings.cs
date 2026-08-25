namespace LagoVista.CloudStorage.DocumentDB
{
    public enum DocumentStorageProviderType
    {
        Cosmos,
        Mongo
    }

    public sealed class DocumentStorageSettings
    {
        public DocumentStorageProviderType Provider { get; set; }
        public string Endpoint { get; set; }
        public string SharedKey { get; set; }
        public string DatabaseName { get; set; }
        public MongoDocumentStorageSettings Mongo { get; set; }
    }
}
