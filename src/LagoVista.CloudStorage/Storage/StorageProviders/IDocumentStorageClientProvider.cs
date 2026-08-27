using LagoVista.CloudStorage.Interfaces;

namespace LagoVista.CloudStorage.Storage.StorageProviders
{
    public interface IDocumentStorageClientProvider
    {
        IDocumentStorageClient GetClient();
    }
}
