using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LagoVista.Core;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class CloudFileStorage
    {
        private readonly IAdminLogger _logger;

        private string _accountId;
        private string _accessKey;
        private string _containerName;

        public CloudFileStorage(string accountId, string accessKey, string containerName, IAdminLogger adminLogger) : this(accountId, accessKey, adminLogger)
        {
            _containerName = containerName;
        }

        public CloudFileStorage(string accountId, string accessKey, IAdminLogger adminLogger)
        {
            _logger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            if (String.IsNullOrEmpty(accessKey))
                throw new ArgumentNullException(nameof(accessKey));

            if (String.IsNullOrEmpty(accountId))
                throw new ArgumentNullException(nameof(accountId));

            _accountId = accountId;
            _accessKey = accessKey;
            _containerName = null;
        }

        public CloudFileStorage(IAdminLogger adminLogger)
        {
            _logger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
        }

        public void InitConnectionSettings(string accountId, string accessKey)
        {
            _accountId = accountId;
            _accessKey = accessKey;

            if (String.IsNullOrEmpty(accountId)) throw new ArgumentNullException(nameof(accountId));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
        }

        private async Task<BlobContainerClient> CreateBlobContainerClient(String containerName )
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountId};AccountKey={_accessKey}";
            var blobClient = new BlobServiceClient(connectionString);
            var blobContainerClient = blobClient.GetBlobContainerClient(containerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            return blobContainerClient;
        }

        public Task<InvokeResult<Uri>> AddFileAsync(string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null)
        {
            if(String.IsNullOrEmpty(_containerName))
                throw new InvalidOperationException("Container name not specified for this instance of CloudFileStorage.  Use the overload that takes a container name.");

            return AddFileAsync(_containerName, fileName, data, contentType, cacheControl);
        }



        public async Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (String.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));
            if (String.IsNullOrEmpty(_accountId)) throw new ArgumentNullException("Must provide account id in constructor, or provide in InitConnectionSettings");
            if (String.IsNullOrEmpty(_accessKey)) throw new ArgumentNullException("Must provide account id in constructor, or provide in InitConnectionSettings");

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var containerClient = await CreateBlobContainerClient(containerName);

            var blobClient = containerClient.GetBlobClient(fileName);
            var header = new BlobHttpHeaders { ContentType = contentType };
            if (cacheControl != null)
                header.CacheControl = cacheControl;

            if (fileName.StartsWith("/"))
                fileName = fileName.TrimStart('/');

            //TODO: Should really encapsulate the idea of retry of an action w/ error reporting
            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;

            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    var binaryData = new BinaryData(data);
                    var blobResult = await blobClient.UploadAsync(binaryData, new BlobUploadOptions { HttpHeaders = header });
                    var statusCode = blobResult.GetRawResponse().Status;
                    if (statusCode < 200 || statusCode > 299)
                        throw new InvalidOperationException($"Invalid response Code {statusCode}");

                    return InvokeResult<Uri>.Create(blobClient.Uri);
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("CloudFileStorage_AddFileAsync", ex, containerName.ToKVP("containerName"));
                        var exceptionResult = InvokeResult.FromException("CloudFileStorage_AddFileAsync", ex);
                        return InvokeResult<Uri>.FromInvokeResult(exceptionResult);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "CloudFileStorage_AddFileAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            //really never get here....
            return InvokeResult<Uri>.FromError("Could not upload file");
        }


        public async Task<InvokeResult<byte[]>> GetFileAsync(string fileName)
        {
            if (String.IsNullOrEmpty(_containerName))
                throw new InvalidOperationException("Container name not specified for this instance of CloudFileStorage.  Use the overload that takes a container name.");

            return await GetFileAsync(_containerName, fileName);
        }

        public async Task<InvokeResult<byte[]>> GetFileAsync(string containerName, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (String.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));
            if (String.IsNullOrEmpty(_accountId)) throw new ArgumentNullException("Must provide account id in constructor, or provide in InitConnectionSettings");
            if (String.IsNullOrEmpty(_accessKey)) throw new ArgumentNullException("Must provide account id in constructor, or provide in InitConnectionSettings");

            if (fileName.StartsWith("/"))
                fileName = fileName.TrimStart('/');

            var containerClient = await CreateBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    var content = await blobClient.DownloadContentAsync();
                    return InvokeResult<byte[]>.Create(content.Value.Content.ToArray());
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("CloudFileStorage_GetFileAsync", ex, containerName.ToKVP("containerName"));
                        return InvokeResult<byte[]>.FromException("CloudFileStorage_GetFileAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "CloudFileStorage_GetFileAsync", "", fileName.ToKVP("fileName"),
                           containerName.ToKVP("containerName"), ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult<byte[]>.FromError("Could not retrieve Media Item");
        }

        public Task<InvokeResult> DeleteFileAsync(string fileName)
        {
            if (String.IsNullOrEmpty(_containerName))
                throw new InvalidOperationException("Container name not specified for this instance of CloudFileStorage.  Use the overload that takes a container name.");

            return DeleteFileAsync(_containerName, fileName);
        }

        public async Task<InvokeResult> DeleteFileAsync(string containerName, string fileName)
        { 
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (String.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));
            if (String.IsNullOrEmpty(_accountId)) throw new ArgumentNullException("Must provide account id in constructor, or provide in InitConnectionSettings");
            if (String.IsNullOrEmpty(_accessKey)) throw new ArgumentNullException("Must provide account id in constructor, or provide in InitConnectionSettings");

            if (fileName.StartsWith("/"))
                fileName = fileName.TrimStart('/');

            var containerClient = await CreateBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    await blobClient.DeleteAsync();
                    return InvokeResult.Success;
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("CloudFileStorage_GetFileAsync", ex, containerName.ToKVP("containerName"));
                        return InvokeResult.FromException("CloudFileStorage_GetFileAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "CloudFileStorage_GetFileAsync", "", fileName.ToKVP("fileName"),
                           containerName.ToKVP("containerName"), ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult.FromError("Could not delete Media Item");
        }
    }
}
