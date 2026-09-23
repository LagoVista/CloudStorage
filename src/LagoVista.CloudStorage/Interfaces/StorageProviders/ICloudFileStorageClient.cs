using LagoVista.Core.Validation;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public enum CloudStorageUrlScope
    {
        Public,
        Internal
    }

    public interface ICloudFileStorageClient
    {
        Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null, bool rejectUpdates = false);

        Task<InvokeResult<Uri>> UpdateFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null);

        Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null);

        Task<InvokeResult<byte[]>> GetFileAsync(string containerName, string fileName);

        Task<InvokeResult<Uri>> CreateReadUrlAsync(string containerName, string fileName, TimeSpan validFor);

        Task<InvokeResult<Uri>> CreateReadUrlAsync(string containerName, string fileName, TimeSpan validFor, CloudStorageUrlScope scope);

        Task<InvokeResult<Uri>> CreateWriteUrlAsync(string containerName, string fileName, string contentType, TimeSpan validFor);

        Task<InvokeResult<Uri>> CreateWriteUrlAsync(string containerName, string fileName, string contentType, TimeSpan validFor, CloudStorageUrlScope scope);

        Task<InvokeResult> DeleteFileAsync(string containerName, string fileName);
    }
}
