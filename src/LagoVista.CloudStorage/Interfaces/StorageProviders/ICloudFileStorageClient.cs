using LagoVista.Core.Validation;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ICloudFileStorageClient
    {
        Task<InvokeResult<Uri>> AddFileAsync(string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null);

        Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null, bool rejectUpdates = false);

        Task<InvokeResult<Uri>> UpdateFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null);

        Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null);

        Task<InvokeResult<byte[]>> GetFileAsync(string fileName);

        Task<InvokeResult<byte[]>> GetFileAsync(string containerName, string fileName);

        Task<InvokeResult<Uri>> CreateReadUrlAsync(string containerName, string fileName, TimeSpan validFor);

        Task<InvokeResult> DeleteFileAsync(string fileName);

        Task<InvokeResult> DeleteFileAsync(string containerName, string fileName);
    }
}
