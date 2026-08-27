using LagoVista.Core.Validation;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Provider-neutral contract for cloud file storage.
    ///
    /// Container and file-name terminology is intentionally retained from the existing
    /// CloudFileStorage API. Providers may map these values to their native concepts
    /// (for example, an S3 bucket and object key).
    /// </summary>
    public interface ICloudFileStorageClient
    {
        Task<InvokeResult<Uri>> AddFileAsync(string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null);

        Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null, bool rejectUpdates = false);

        Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null);

        Task<InvokeResult<Uri>> UpdateFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null);

        Task<InvokeResult<byte[]>> GetFileAsync(string fileName);

        Task<InvokeResult<byte[]>> GetFileAsync(string containerName, string fileName);

        Task<InvokeResult> DeleteFileAsync(string fileName);

        Task<InvokeResult> DeleteFileAsync(string containerName, string fileName);
    }
}
