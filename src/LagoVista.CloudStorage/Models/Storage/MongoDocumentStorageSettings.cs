namespace LagoVista.CloudStorage.DocumentDB
{
    /// <summary>
    /// Resolved Mongo destination for one logical document-storage operation.
    /// Server connection configuration lives in MongoDocumentStorageConnectionSettings.
    /// </summary>
    public sealed class MongoDocumentStorageSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
