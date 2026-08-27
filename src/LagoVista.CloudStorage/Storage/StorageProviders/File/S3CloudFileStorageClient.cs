using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.Core;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Minio;
using Minio.DataModel.Args;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.File
{
    /// <summary>
    /// S3-compatible implementation of the provider-neutral cloud file storage contract.
    ///
    /// This client intentionally depends only on the S3 protocol client and provider-neutral
    /// connection settings. SeaweedFS-specific concepts must not leak into this class.
    /// </summary>
    public class S3CloudFileStorageClient : ICloudFileStorageClient
    {
        private const int NumberRetries = 5;
        private const int MaxPresignedUrlLifetimeSeconds = 7 * 24 * 60 * 60;
        private const int RetryBaseDelayMilliseconds = 250;
        private const int RetryJitterMilliseconds = 125;

        private static readonly object RetryRandomSync = new object();
        private static readonly Random RetryRandom = new Random();
        private static readonly HashSet<string> NonRetryableExceptionTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "AccessDeniedException",
            "AuthorizationException",
            "CredentialsProviderException",
            "EntityTooLargeException",
            "ForbiddenException",
            "InvalidBucketNameException",
            "InvalidEndpointException",
            "InvalidAccessKeyException",
            "InvalidSecretKeyException",
            "InvalidObjectNameException"
        };

        private readonly IAdminLogger _logger;
        private readonly IS3ObjectStorageConnectionSettings _settings;
        private readonly IMinioClient _client;
        private readonly IMinioClient _readUrlClient;
        private readonly string _containerName;

        public S3CloudFileStorageClient(IS3ObjectStorageConnectionSettings settings, IAdminLogger adminLogger)
            : this(settings, null, adminLogger)
        {
        }

        public S3CloudFileStorageClient(IS3ObjectStorageConnectionSettings settings, string containerName, IAdminLogger adminLogger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            _containerName = String.IsNullOrWhiteSpace(containerName) ? null : containerName.Trim();

            _client = BuildClient(_settings.Host, _settings.Port, _settings.UseTls);
            _readUrlClient = BuildClient(_settings.PublicHost, _settings.PublicPort, _settings.PublicUseTls);
        }

        public Task<InvokeResult<Uri>> AddFileAsync(string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null)
        {
            if (String.IsNullOrEmpty(_containerName))
                throw new InvalidOperationException("Container name not specified for this instance of S3CloudFileStorageClient. Use the overload that takes a container name.");

            return AddFileAsync(_containerName, fileName, data, contentType, cacheControl);
        }

        public async Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, byte[] data, string contentType = "application/octet-stream", string cacheControl = null, bool rejectUpdates = false)
        {
            ValidateFileArguments(containerName, fileName);
            if (data == null) throw new ArgumentNullException(nameof(data));

            fileName = NormalizeObjectName(fileName);

            if (rejectUpdates)
            {
                return InvokeResult<Uri>.FromError(
                    "Atomic create-only S3 uploads are not yet implemented. rejectUpdates requires an If-None-Match:* conditional PUT and must not be emulated with a check-then-write operation.");
            }

            var sw = Stopwatch.StartNew();
            _logger.Trace($"{this.Tag()} - uploading file to S3 storage", fileName.ToKVP("fileName"), containerName.ToKVP("containerName"));

            for (var retryCount = 1; retryCount <= NumberRetries; retryCount++)
            {
                try
                {
                    await EnsureBucketExistsAsync(containerName);

                    using (var stream = new MemoryStream(data, false))
                    {
                        var args = new PutObjectArgs()
                            .WithBucket(containerName)
                            .WithObject(fileName)
                            .WithStreamData(stream)
                            .WithObjectSize(data.LongLength)
                            .WithContentType(contentType);

                        if (!String.IsNullOrWhiteSpace(cacheControl))
                        {
                            args = args.WithHeaders(new Dictionary<string, string>
                            {
                                ["Cache-Control"] = cacheControl
                            });
                        }

                        await _client.PutObjectAsync(args);
                    }

                    _logger.Trace($"{this.Tag()} - uploaded file to S3 storage",
                        sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"),
                        containerName.ToKVP("containerName"),
                        fileName.ToKVP("fileName"));

                    return InvokeResult<Uri>.Create(BuildObjectUri(containerName, fileName));
                }
                catch (Exception ex)
                {
                    if (!ShouldRetry(ex) || retryCount == NumberRetries)
                    {
                        _logger.AddException(this.Tag(), ex,
                            containerName.ToKVP("containerName"),
                            fileName.ToKVP("fileName"));
                        var exceptionResult = InvokeResult.FromException("[S3CloudFileStorageClient__AddFileAsync]", ex);
                        return InvokeResult<Uri>.FromInvokeResult(exceptionResult);
                    }

                    var delayMs = GetRetryDelayMilliseconds(retryCount);
                    LogRetry("retry S3 upload", ex, containerName, fileName, retryCount, delayMs);
                    await Task.Delay(delayMs);
                }
            }

            return InvokeResult<Uri>.FromError("Could not upload file");
        }

        public Task<InvokeResult<Uri>> UpdateFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null)
        {
            return AddFileAsync(containerName, fileName, data, contentType, cacheControl);
        }

        public Task<InvokeResult<Uri>> AddFileAsync(string containerName, string fileName, string data, string contentType = "text/plain", string cacheControl = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return AddFileAsync(containerName, fileName, Encoding.UTF8.GetBytes(data), contentType, cacheControl);
        }

        public Task<InvokeResult<byte[]>> GetFileAsync(string fileName)
        {
            if (String.IsNullOrEmpty(_containerName))
                throw new InvalidOperationException("Container name not specified for this instance of S3CloudFileStorageClient. Use the overload that takes a container name.");

            return GetFileAsync(_containerName, fileName);
        }

        public async Task<InvokeResult<byte[]>> GetFileAsync(string containerName, string fileName)
        {
            ValidateFileArguments(containerName, fileName);
            fileName = NormalizeObjectName(fileName);

            for (var retryCount = 1; retryCount <= NumberRetries; retryCount++)
            {
                try
                {
                    await EnsureBucketExistsAsync(containerName);
                    _logger.Trace($"{this.Tag()} - getting S3 object", containerName.ToKVP("containerName"), fileName.ToKVP("fileName"));

                    using (var output = new MemoryStream())
                    {
                        var args = new GetObjectArgs()
                            .WithBucket(containerName)
                            .WithObject(fileName)
                            .WithCallbackStream(async (stream, cancellationToken) =>
                            {
                                await stream.CopyToAsync(output, 81920, cancellationToken);
                            });

                        await _client.GetObjectAsync(args);

                        _logger.Trace($"{this.Tag()} - got S3 object", containerName.ToKVP("containerName"), fileName.ToKVP("fileName"));
                        return InvokeResult<byte[]>.Create(output.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    if (!ShouldRetry(ex) || retryCount == NumberRetries)
                    {
                        _logger.AddException(this.Tag(), ex,
                            containerName.ToKVP("containerName"),
                            fileName.ToKVP("fileName"));
                        return InvokeResult<byte[]>.FromException("S3CloudFileStorageClient_GetFileAsync", ex);
                    }

                    var delayMs = GetRetryDelayMilliseconds(retryCount);
                    LogRetry("retry S3 get", ex, containerName, fileName, retryCount, delayMs);
                    await Task.Delay(delayMs);
                }
            }

            return InvokeResult<byte[]>.FromError("Could not retrieve file");
        }

        public async Task<InvokeResult<Uri>> CreateReadUrlAsync(string containerName, string fileName, TimeSpan validFor)
        {
            ValidateFileArguments(containerName, fileName);
            fileName = NormalizeObjectName(fileName);

            if (validFor <= TimeSpan.Zero)
                return InvokeResult<Uri>.FromError("Signed read URL lifetime must be greater than zero.");

            var expirySeconds = (long)Math.Ceiling(validFor.TotalSeconds);
            if (expirySeconds > MaxPresignedUrlLifetimeSeconds)
                return InvokeResult<Uri>.FromError("S3 signed read URLs cannot be valid for more than seven days.");

            try
            {
                await _client.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(containerName)
                    .WithObject(fileName));

                var signedUrl = await _readUrlClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                    .WithBucket(containerName)
                    .WithObject(fileName)
                    .WithExpiry((int)expirySeconds));

                if (!Uri.TryCreate(signedUrl, UriKind.Absolute, out var uri))
                    return InvokeResult<Uri>.FromError("S3 client returned an invalid signed read URL.");

                return InvokeResult<Uri>.Create(uri);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex,
                    containerName.ToKVP("containerName"),
                    fileName.ToKVP("fileName"));
                return InvokeResult<Uri>.FromException("[S3CloudFileStorageClient__CreateReadUrlAsync]", ex);
            }
        }

        public Task<InvokeResult> DeleteFileAsync(string fileName)
        {
            if (String.IsNullOrEmpty(_containerName))
                throw new InvalidOperationException("Container name not specified for this instance of S3CloudFileStorageClient. Use the overload that takes a container name.");

            return DeleteFileAsync(_containerName, fileName);
        }

        public async Task<InvokeResult> DeleteFileAsync(string containerName, string fileName)
        {
            ValidateFileArguments(containerName, fileName);
            fileName = NormalizeObjectName(fileName);

            for (var retryCount = 1; retryCount <= NumberRetries; retryCount++)
            {
                try
                {
                    await EnsureBucketExistsAsync(containerName);
                    var sw = Stopwatch.StartNew();
                    _logger.Trace($"{this.Tag()} - deleting S3 object", containerName.ToKVP("containerName"), fileName.ToKVP("fileName"));

                    var args = new RemoveObjectArgs()
                        .WithBucket(containerName)
                        .WithObject(fileName);

                    await _client.RemoveObjectAsync(args);

                    _logger.Trace($"{this.Tag()} - deleted S3 object",
                        sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"),
                        containerName.ToKVP("containerName"),
                        fileName.ToKVP("fileName"));
                    return InvokeResult.Success;
                }
                catch (Exception ex)
                {
                    if (!ShouldRetry(ex) || retryCount == NumberRetries)
                    {
                        _logger.AddException(this.Tag(), ex,
                            containerName.ToKVP("containerName"),
                            fileName.ToKVP("fileName"));
                        return InvokeResult.FromException("[S3CloudFileStorageClient__DeleteFileAsync]", ex);
                    }

                    var delayMs = GetRetryDelayMilliseconds(retryCount);
                    LogRetry("retry S3 delete", ex, containerName, fileName, retryCount, delayMs);
                    await Task.Delay(delayMs);
                }
            }

            return InvokeResult.FromError("Could not delete file");
        }

        private IMinioClient BuildClient(string host, int port, bool useTls)
        {
            IMinioClient builder = new MinioClient();
            var isDefaultPort = (useTls && port == 443) || (!useTls && port == 80);

            builder = isDefaultPort ? builder.WithEndpoint(host) : builder.WithEndpoint(host, port);

            builder = builder.WithCredentials(_settings.AccessKey, _settings.SecretKey).WithSSL(useTls);

            if (!String.IsNullOrWhiteSpace(_settings.Region))
                builder = builder.WithRegion(_settings.Region);

            return builder.Build();
        }

        private async Task EnsureBucketExistsAsync(string bucketName)
        {
            var existsArgs = new BucketExistsArgs().WithBucket(bucketName);
            if (await _client.BucketExistsAsync(existsArgs)) return;

            try
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                if (!String.IsNullOrWhiteSpace(_settings.Region))
                    makeBucketArgs = makeBucketArgs.WithLocation(_settings.Region);

                await _client.MakeBucketAsync(makeBucketArgs);
            }
            catch
            {
                // Bucket creation is idempotent at the storage-contract level. If another caller
                // created it between the existence check and MakeBucketAsync, continue normally.
                if (await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName))) return;
                throw;
            }
        }

        private Uri BuildObjectUri(string bucketName, string objectName)
        {
            var scheme = _settings.UseTls ? "https" : "http";
            var port = (_settings.UseTls && _settings.Port == 443) || (!_settings.UseTls && _settings.Port == 80)
                ? String.Empty
                : $":{_settings.Port}";
            var escapedPath = String.Join("/", objectName.Split('/').Select(Uri.EscapeDataString));
            return new Uri($"{scheme}://{_settings.Host}{port}/{Uri.EscapeDataString(bucketName)}/{escapedPath}");
        }

        private static bool ShouldRetry(Exception ex)
        {
            // Fail fast only for errors that are clearly configuration/auth/request problems.
            // Unknown MinIO/S3 failures continue to retry so provider-specific transient failures
            // do not accidentally become less resilient as the S3 implementation evolves.
            return !NonRetryableExceptionTypes.Contains(ex.GetType().Name);
        }

        private static int GetRetryDelayMilliseconds(int retryCount)
        {
            var exponent = Math.Min(Math.Max(retryCount - 1, 0), 3);
            var delay = RetryBaseDelayMilliseconds * (1 << exponent);
            int jitter;
            lock (RetryRandomSync)
            {
                jitter = RetryRandom.Next(0, RetryJitterMilliseconds + 1);
            }

            return delay + jitter;
        }

        private void LogRetry(string message, Exception ex, string containerName, string fileName, int retryCount, int delayMs)
        {
            _logger.AddCustomEvent(
                LagoVista.Core.PlatformSupport.LogLevel.Warning,
                this.Tag(),
                message,
                fileName.ToKVP("fileName"),
                containerName.ToKVP("containerName"),
                ex.Message.ToKVP("exceptionMessage"),
                ex.GetType().Name.ToKVP("exceptionType"),
                retryCount.ToString().ToKVP("retryCount"),
                delayMs.ToString().ToKVP("retryDelayMs"));
        }

        private static void ValidateFileArguments(string containerName, string fileName)
        {
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (String.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));
        }

        private static string NormalizeObjectName(string fileName)
        {
            return fileName.StartsWith("/", StringComparison.Ordinal) ? fileName.TrimStart('/') : fileName;
        }
    }
}
