using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LagoVista.Core;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class CloudFileStorage
    {
        private readonly IAdminLogger _logger;

        private readonly string _accountId;
        private readonly string _accessKey;
        private readonly string _containerName;

        public CloudFileStorage(string accountId, string accessKey, string containerName, IAdminLogger adminLogger)
        {
            _logger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            if (String.IsNullOrEmpty(accessKey))
                throw new ArgumentNullException(nameof(accessKey));

            if (String.IsNullOrEmpty(accountId))
                throw new ArgumentNullException(nameof(accountId));

            if (String.IsNullOrEmpty(containerName))
                throw new ArgumentNullException(nameof(containerName));


            _accountId = accountId;
            _accessKey = accessKey;
            _containerName = containerName;
        }

        private async Task<BlobContainerClient> CreateBlobContainerClient(String containerName = null)
        {
            if (string.IsNullOrEmpty(containerName)) containerName = _containerName;

            var baseuri = $"https://{_accountId}.blob.core.windows.net";

            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountId};AccountKey={_accessKey}";
            var blobClient = new BlobServiceClient(connectionString);
            try
            {
                var blobContainerClient = blobClient.GetBlobContainerClient(_containerName);
                return blobContainerClient;
            }
            catch (Exception)
            {
                var container = await blobClient.CreateBlobContainerAsync(_containerName);

                return container.Value;
            }
        }


        public async Task<InvokeResult<Uri>> AddFileAsync(string fileName, byte[] data, string contentType = "application/octet-stream")
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var containerClient = await CreateBlobContainerClient(_containerName);

            var blobClient = containerClient.GetBlobClient(fileName);
            var header = new BlobHttpHeaders { ContentType = contentType };

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
                        _logger.AddException("CloudFileStorage_AddFileAsync", ex, _containerName.ToKVP("containerName"));
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
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (fileName.StartsWith("/"))
                fileName = fileName.TrimStart('/');

            var containerClient = await CreateBlobContainerClient(_containerName);
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
                        _logger.AddException("CloudFileStorage_GetFileAsync", ex, _containerName.ToKVP("containerName"));
                        return InvokeResult<byte[]>.FromException("CloudFileStorage_GetFileAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "CloudFileStorage_GetFileAsync", "", fileName.ToKVP("fileName"),
                           _containerName.ToKVP("containerName"), ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult<byte[]>.FromError("Could not retrieve Media Item");
        }

        public async Task<InvokeResult> DeleteFileAsync(string fileName)
        {

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (fileName.StartsWith("/"))
                fileName = fileName.TrimStart('/');

            var containerClient = await CreateBlobContainerClient(_containerName);
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
                        _logger.AddException("CloudFileStorage_GetFileAsync", ex, _containerName.ToKVP("containerName"));
                        return InvokeResult.FromException("CloudFileStorage_GetFileAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "CloudFileStorage_GetFileAsync", "", fileName.ToKVP("fileName"),
                           _containerName.ToKVP("containerName"), ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult.FromError("Could not delete Media Item");
        }
    }
}
