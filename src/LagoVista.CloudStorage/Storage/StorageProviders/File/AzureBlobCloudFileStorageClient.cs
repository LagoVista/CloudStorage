using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Azure Blob Storage implementation of the provider-neutral cloud file storage contract.
    ///
    /// This adapter intentionally delegates normal file operations to the existing CloudFileStorage
    /// implementation while exposing provider-neutral temporary read URLs for migration parity.
    /// </summary>
    public class AzureBlobCloudFileStorageClient : CloudFileStorage, ICloudFileStorageClient
    {
        private string _accountId;
        private string _accessKey;

        public AzureBlobCloudFileStorageClient(string accountId, string accessKey, string containerName, IAdminLogger adminLogger)
            : base(accountId, accessKey, containerName, adminLogger)
        {
            _accountId = accountId;
            _accessKey = accessKey;
        }

        public AzureBlobCloudFileStorageClient(string accountId, string accessKey, IAdminLogger adminLogger)
            : base(accountId, accessKey, adminLogger)
        {
            _accountId = accountId;
            _accessKey = accessKey;
        }

        public AzureBlobCloudFileStorageClient(IAdminLogger adminLogger)
            : base(adminLogger)
        {
        }

        public new void InitConnectionSettings(string accountId, string accessKey)
        {
            base.InitConnectionSettings(accountId, accessKey);
            _accountId = accountId;
            _accessKey = accessKey;
        }

        public async Task<InvokeResult<Uri>> CreateReadUrlAsync(string containerName, string fileName, TimeSpan validFor)
        {
            if (String.IsNullOrWhiteSpace(containerName)) throw new ArgumentNullException(nameof(containerName));
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (String.IsNullOrWhiteSpace(_accountId) || String.IsNullOrWhiteSpace(_accessKey))
                return InvokeResult<Uri>.FromError("Azure storage connection settings are required to create a temporary read URL.");
            if (validFor <= TimeSpan.Zero)
                return InvokeResult<Uri>.FromError("Signed read URL lifetime must be greater than zero.");

            fileName = fileName.TrimStart('/');

            try
            {
                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountId};AccountKey={_accessKey}";
                var serviceClient = new BlobServiceClient(connectionString);
                var containerClient = serviceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                var exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                    return InvokeResult<Uri>.FromError($"Could not find file '{fileName}' in container '{containerName}'.");

                if (!blobClient.CanGenerateSasUri)
                    return InvokeResult<Uri>.FromError($"Azure storage client cannot generate a signed read URL for '{fileName}'.");

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = fileName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.Add(validFor)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return InvokeResult<Uri>.Create(blobClient.GenerateSasUri(sasBuilder));
            }
            catch (Exception ex)
            {
                return InvokeResult<Uri>.FromException("[AzureBlobCloudFileStorageClient__CreateReadUrlAsync]", ex);
            }
        }
    }
}
