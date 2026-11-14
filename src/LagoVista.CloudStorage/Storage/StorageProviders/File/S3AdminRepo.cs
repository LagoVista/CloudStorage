using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using Minio;
using Minio.DataModel.Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.File
{
    /// <summary>
    /// Read-only object inventory backed by the existing S3ObjectStorage account.
    /// No bucket creation, uploads, deletes, or caller-supplied connections.
    /// </summary>
    public interface IS3AdminRepo
    {
        Task<IReadOnlyList<string>> GetBucketsAsync(CancellationToken ct = default);
        Task<S3AdminObjectList> ListObjectsAsync(string bucket, string prefix, CancellationToken ct = default);
        Task<S3AdminObjectInfo> GetObjectAsync(string bucket, string key, CancellationToken ct = default);
    }

    public sealed class S3AdminRepo : IS3AdminRepo
    {
        public const int MaximumItems = 200;
        private readonly IMinioClient _client;

        public S3AdminRepo(IS3ObjectStorageConnectionSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            IMinioClient builder = new MinioClient();
            var standardPort = (settings.UseTls && settings.Port == 443) || (!settings.UseTls && settings.Port == 80);
            builder = standardPort ? builder.WithEndpoint(settings.Host) : builder.WithEndpoint(settings.Host, settings.Port);
            builder = builder.WithCredentials(settings.AccessKey, settings.SecretKey).WithSSL(settings.UseTls);
            if (!String.IsNullOrWhiteSpace(settings.Region))
                builder = builder.WithRegion(settings.Region);
            _client = builder.Build();
        }

        public static string NormalizePrefix(string prefix)
        {
            if (String.IsNullOrWhiteSpace(prefix)) return String.Empty;
            if (prefix.Length > 1024 || prefix.IndexOf('\\') >= 0)
                throw new ArgumentException("Invalid S3 prefix.", nameof(prefix));
            prefix = prefix.TrimStart('/');
            return prefix.Length == 0 || prefix.EndsWith("/", StringComparison.Ordinal) ? prefix : prefix + "/";
        }

        public static void ValidateBucket(string bucket)
        {
            if (String.IsNullOrWhiteSpace(bucket) || bucket.Length > 253 ||
                bucket.IndexOf('/') >= 0 || bucket.IndexOf('\\') >= 0)
                throw new ArgumentException("A valid bucket is required.", nameof(bucket));
        }

        public static void ValidateKey(string key)
        {
            if (String.IsNullOrWhiteSpace(key) || key.Length > 2048 || key.StartsWith("/", StringComparison.Ordinal))
                throw new ArgumentException("An object key is required.", nameof(key));
        }

        public async Task<IReadOnlyList<string>> GetBucketsAsync(CancellationToken ct = default)
        {
            var response = await _client.ListBucketsAsync(ct).ConfigureAwait(false);
            return response.Buckets.Select(b => b.Name).OrderBy(b => b, StringComparer.Ordinal).ToList();
        }

        public async Task<S3AdminObjectList> ListObjectsAsync(string bucket, string prefix, CancellationToken ct = default)
        {
            ValidateBucket(bucket);
            prefix = NormalizePrefix(prefix);
            var result = new S3AdminObjectList { Bucket = bucket, Prefix = prefix };
            var args = new ListObjectsArgs().WithBucket(bucket).WithPrefix(prefix).WithRecursive(false);
            await foreach (var item in _client.ListObjectsEnumAsync(args, ct))
            {
                ct.ThrowIfCancellationRequested();
                if (result.Items.Count >= MaximumItems)
                {
                    result.Truncated = true;
                    break;
                }
                result.Items.Add(new S3AdminObjectInfo
                {
                    Key = item.Key,
                    IsDirectory = item.IsDir,
                    SizeBytes = Convert.ToInt64(item.Size),
                    LastModified = item.LastModified,
                    ETag = item.ETag,
                    ContentType = item.ContentType
                });
            }
            return result;
        }

        public async Task<S3AdminObjectInfo> GetObjectAsync(string bucket, string key, CancellationToken ct = default)
        {
            ValidateBucket(bucket);
            ValidateKey(key);
            var stat = await _client.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(key), ct).ConfigureAwait(false);
            return new S3AdminObjectInfo
            {
                Key = key,
                IsDirectory = false,
                SizeBytes = Convert.ToInt64(stat.Size),
                LastModified = stat.LastModified.ToString(),
                ETag = stat.ETag,
                ContentType = stat.ContentType
            };
        }
    }
}
