using LagoVista.CloudStorage.Interfaces;
using LagoVista.IoT.Logging.Loggers;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Azure Blob Storage implementation of the provider-neutral cloud file storage contract.
    ///
    /// This adapter intentionally delegates to the existing CloudFileStorage implementation so
    /// the provider seam can be introduced without changing legacy callers or Azure behavior.
    /// </summary>
    public class AzureBlobCloudFileStorageClient : CloudFileStorage, ICloudFileStorageClient
    {
        public AzureBlobCloudFileStorageClient(string accountId, string accessKey, string containerName, IAdminLogger adminLogger)
            : base(accountId, accessKey, containerName, adminLogger)
        {
        }

        public AzureBlobCloudFileStorageClient(string accountId, string accessKey, IAdminLogger adminLogger)
            : base(accountId, accessKey, adminLogger)
        {
        }

        public AzureBlobCloudFileStorageClient(IAdminLogger adminLogger)
            : base(adminLogger)
        {
        }
    }
}
