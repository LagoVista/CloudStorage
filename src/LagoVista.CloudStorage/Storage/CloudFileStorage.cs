using LagoVista.Core;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
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

        private CloudBlobClient CreateBlobClient()
        {
            var baseuri = $"https://{_accountId}.blob.core.windows.net";

            var uri = new Uri(baseuri);
            return new CloudBlobClient(uri, new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(_accountId, _accessKey));
        }

        private async Task<InvokeResult<CloudBlobContainer>> GetStorageContainerAsync(string containerName)
        {
            var client = CreateBlobClient();
            var container = client.GetContainerReference(containerName);
            try
            {
                var options = new BlobRequestOptions()
                {
                    MaximumExecutionTime = TimeSpan.FromSeconds(15)
                };

                var opContext = new OperationContext();
                await container.CreateIfNotExistsAsync(options, opContext);
                return InvokeResult<CloudBlobContainer>.Create(container);
            }
            catch (ArgumentException ex)
            {
                _logger.AddException("CloudFileStorage_GetStorageContainerAsync", ex, containerName.ToKVP(nameof(containerName)));
                return InvokeResult<CloudBlobContainer>.FromException("CloudFileStorage_GetStorageContainerAsync_InitAsync", ex);
            }
            catch (StorageException ex)
            {
                _logger.AddException("ReportsLibraryRepo_GetStorageContainerAsync", ex, containerName.ToKVP(nameof(containerName)));
                return InvokeResult<CloudBlobContainer>.FromException("CloudFileStorage_GetStorageContainerAsync", ex);
            }
        }

        public async Task<InvokeResult<Uri>> AddFileAsync(string fileName, byte[] data, string contentType = "application/octet-stream")
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if(data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var result = await GetStorageContainerAsync(_containerName);
            if (!result.Successful)
            {
                return InvokeResult<Uri>.FromInvokeResult(result.ToInvokeResult());
            }

            var container = result.Result;

            if (fileName.StartsWith("/"))
                fileName = fileName.TrimStart('/');

            var blob = container.GetBlockBlobReference(fileName);
            blob.Properties.ContentType = contentType;

            //TODO: Should really encapsulate the idea of retry of an action w/ error reporting
            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            var stream = new MemoryStream(data);
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream);
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

            return InvokeResult<Uri>.Create(blob.Uri);
        }

        public async Task<InvokeResult<byte[]>> GetFileAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (fileName.StartsWith("/"))
                fileName = fileName.TrimStart('/');

            var result = await GetStorageContainerAsync(_containerName);
            if (!result.Successful)
            {
                return InvokeResult<byte[]>.FromInvokeResult(result.ToInvokeResult());
            }

            var container = result.Result;

            var blob = container.GetBlockBlobReference(fileName);
            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    //TODO: We shouldn't likely return a byte array here probably return access to the response object and stream the bytes as they are downloaded, current architecture doesn't support...
                    using (var ms = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(ms);
                        return InvokeResult<byte[]>.Create(ms.ToArray());
                    }
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

            var result = await GetStorageContainerAsync(_containerName);
            if (!result.Successful)
            {
                return result.ToInvokeResult();
            }

            var container = result.Result;

            var blob = container.GetBlockBlobReference(fileName);
            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    await blob.DeleteAsync();
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
