namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Selects the physical provider used by a TableStorageBase repository.
    /// Azure Table Storage remains the default so existing repositories continue
    /// to behave exactly as they do today until they explicitly opt in.
    /// </summary>
    public enum FlatStorageProvider
    {
        AzureTableStorage = 0,
        Cassandra = 1,
        MongoDB = 2
    }
}
