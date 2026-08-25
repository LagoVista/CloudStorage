using LagoVista.CloudStorage.Storage;

namespace LagoVista.CloudStorage.Tests
{
    internal class DocumentCollectionFactory
    {
        private CosmosClientProvider shared;

        public DocumentCollectionFactory(CosmosClientProvider shared)
        {
            this.shared = shared;
        }
    }
}