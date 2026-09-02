using Azure.Storage.Blobs;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using Minio;
using Minio.DataModel.Args;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LagoVista.StorageMigration;

public sealed class ObjectMigrationProgress
{
    public int RunObjectsWritten { get; init; }
    public long RunBytesWritten { get; init; }
    public long TotalObjectsWritten { get; init; }
    public long TotalBytesWritten { get; init; }
    public string Container { get; init; } = String.Empty;
    public string ObjectKey { get; init; } = String.Empty;
    public TimeSpan Elapsed { get; init; }
}

public sealed class AzureBlobToS3Migration
{
    public const string MigrationKey = "azure-blob-to-s3";
    private const int ObjectCopyAttempts = 5;
    private const string EmptyPayloadSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private static readonly string DefinitionSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("azure-blob-to-s3-v1")));
    private static readonly HttpClient S3HttpClient = new();

    private readonly BlobServiceClient _source;
    private readonly IMinioClient _target;
    private readonly IS3ObjectStorageConnectionSettings _settings;
    private readonly IMigrationStateStore _stateStore;

    public AzureBlobToS3Migration(
        string azureConnectionString,
        IS3ObjectStorageConnectionSettings settings,
        IMigrationStateStore stateStore)
    {
        _source = new BlobServiceClient(azureConnectionString);
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));

        var builder = new MinioClient()
            .WithEndpoint(settings.Host, settings.Port)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseTls);

        if (!String.IsNullOrWhiteSpace(settings.Region))
            builder = builder.WithRegion(settings.Region);

        _target = builder.Build();
    }

    public async Task<MigrationRunState> ExecuteAsync(
        int? maxObjects = null,
        Action<ObjectMigrationProgress>? progress = null,
        int batchSize = 10,
        int parallelism = 8,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (parallelism <= 0) throw new ArgumentOutOfRangeException(nameof(parallelism));

        var state = await _stateStore.GetAsync(MigrationKey, cancellationToken).ConfigureAwait(false)
            ?? NewState();

        if (!String.Equals(state.DefinitionSha256, DefinitionSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Object migration checkpoint does not match the current migration definition.");

        if (String.Equals(state.State, "Completed", StringComparison.OrdinalIgnoreCase))
            return state;

        state.State = "Running";
        state.CompletedDate = null;
        await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);

        var containers = new List<string>();
        await foreach (var container in _source.GetBlobContainersAsync(cancellationToken: cancellationToken))
            containers.Add(container.Name);
        containers.Sort(StringComparer.Ordinal);

        var copiedThisRun = 0;
        var bytesCopiedThisRun = 0L;
        var stopwatch = Stopwatch.StartNew();

        foreach (var containerName in containers)
        {
            if (!ShouldProcessContainer(containerName, state.CurrentTable))
                continue;

            var container = _source.GetBlobContainerClient(containerName);
            var resumingCurrentContainer = String.Equals(containerName, state.CurrentTable, StringComparison.Ordinal);
            var sawBlob = false;
            var bucketEnsured = false;
            var batch = new List<ObjectCopyItem>(batchSize);

            await foreach (var blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                sawBlob = true;

                if (resumingCurrentContainer && !String.IsNullOrEmpty(state.HeadRowKey) &&
                    StringComparer.Ordinal.Compare(blob.Name, state.HeadRowKey) <= 0)
                    continue;

                if (!bucketEnsured)
                {
                    await EnsureBucketAsync(containerName, cancellationToken).ConfigureAwait(false);
                    bucketEnsured = true;
                }

                if (maxObjects.HasValue && copiedThisRun + batch.Count >= maxObjects.Value)
                    break;

                batch.Add(new ObjectCopyItem(
                    blob.Name,
                    blob.Properties.ContentLength ?? 0,
                    blob.Properties.ContentType,
                    blob.Properties.CacheControl));

                if (batch.Count < batchSize)
                    continue;

                var result = await CopyBatchAsync(container, containerName, batch, parallelism, cancellationToken).ConfigureAwait(false);
                CommitBatch(state, containerName, result, ref copiedThisRun, ref bytesCopiedThisRun);
                await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
                ReportProgress(progress, copiedThisRun, bytesCopiedThisRun, state, stopwatch.Elapsed);
                batch.Clear();

                if (maxObjects.HasValue && copiedThisRun >= maxObjects.Value)
                {
                    state.State = "Paused";
                    await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
                    return state;
                }
            }

            if (batch.Count > 0)
            {
                var result = await CopyBatchAsync(container, containerName, batch, parallelism, cancellationToken).ConfigureAwait(false);
                CommitBatch(state, containerName, result, ref copiedThisRun, ref bytesCopiedThisRun);
                await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
                ReportProgress(progress, copiedThisRun, bytesCopiedThisRun, state, stopwatch.Elapsed);
                batch.Clear();
            }

            if (maxObjects.HasValue && copiedThisRun >= maxObjects.Value)
            {
                state.State = "Paused";
                await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
                return state;
            }

            if (!sawBlob)
            {
                state.CurrentTable = containerName;
                state.HeadPartitionKey = containerName;
                state.HeadRowKey = null;
                await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
            }
        }

        state.State = "Completed";
        state.CompletedDate = DateTime.UtcNow;
        state.LastUpdatedDate = DateTime.UtcNow;
        await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
        ReportProgress(progress, copiedThisRun, bytesCopiedThisRun, state, stopwatch.Elapsed);
        return state;
    }

    private async Task<ObjectCopyBatchResult> CopyBatchAsync(
        BlobContainerClient container,
        string containerName,
        IReadOnlyList<ObjectCopyItem> batch,
        int parallelism,
        CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(parallelism, parallelism);

        var tasks = batch.Select(async item =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await CopyObjectAsync(container, containerName, item, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }).ToArray();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var failed = results.Where(result => result.Error != null).ToArray();
        if (failed.Length > 0)
        {
            var state = await _stateStore.GetAsync(MigrationKey, cancellationToken).ConfigureAwait(false) ?? NewState();
            state.RecordsFailed += failed.Length;
            state.State = "Failed";
            state.LastUpdatedDate = DateTime.UtcNow;
            await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);

            var first = failed[0];
            throw new InvalidOperationException(
                $"Object batch failed for '{containerName}/{first.Item.Name}' after {ObjectCopyAttempts} attempts. " +
                $"ListedLength={first.Item.ContentLength:N0}; ContentType='{first.Item.ContentType ?? "<none>"}'; " +
                $"CacheControl='{first.Item.CacheControl ?? "<none>"}'. " +
                "The batch checkpoint was not advanced and will be replayed safely.",
                first.Error);
        }

        return new ObjectCopyBatchResult(
            results.Select(result => result.Item).ToArray(),
            results.Sum(result => result.Item.ContentLength));
    }

    private async Task<ObjectCopyResult> CopyObjectAsync(
        BlobContainerClient container,
        string containerName,
        ObjectCopyItem item,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= ObjectCopyAttempts; attempt++)
        {
            try
            {
                var contentType = String.IsNullOrWhiteSpace(item.ContentType)
                    ? "application/octet-stream"
                    : item.ContentType;

                if (item.ContentLength == 0)
                {
                    await PutEmptyObjectAsync(
                        containerName,
                        item.Name,
                        contentType,
                        item.CacheControl,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var sourceBlob = container.GetBlobClient(item.Name);
                    var download = await sourceBlob.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    await using var stream = download.Value.Content;

                    var put = new PutObjectArgs()
                        .WithBucket(containerName)
                        .WithObject(item.Name)
                        .WithStreamData(stream)
                        .WithObjectSize(item.ContentLength)
                        .WithContentType(contentType);

                    if (!String.IsNullOrWhiteSpace(item.CacheControl))
                    {
                        put = put.WithHeaders(new Dictionary<string, string>
                        {
                            ["Cache-Control"] = item.CacheControl
                        });
                    }

                    await _target.PutObjectAsync(put, cancellationToken).ConfigureAwait(false);
                }

                return new ObjectCopyResult(item, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (attempt == ObjectCopyAttempts)
                    break;

                var delay = TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return new ObjectCopyResult(item, lastError ?? new InvalidOperationException("Object copy failed without an exception."));
    }

    private async Task PutEmptyObjectAsync(
        string bucketName,
        string objectName,
        string contentType,
        string? cacheControl,
        CancellationToken cancellationToken)
    {
        var region = String.IsNullOrWhiteSpace(_settings.Region) ? "us-east-1" : _settings.Region.Trim();
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var canonicalUri = "/" + EncodeS3Path(bucketName) + "/" + EncodeS3Path(objectName);
        var defaultPort = _settings.UseTls ? 443 : 80;
        var hostHeader = _settings.Port == defaultPort
            ? _settings.Host
            : $"{_settings.Host}:{_settings.Port}";
        var canonicalHeaders =
            $"host:{hostHeader}\n" +
            $"x-amz-content-sha256:{EmptyPayloadSha256}\n" +
            $"x-amz-date:{amzDate}\n";
        const string signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest =
            $"PUT\n{canonicalUri}\n\n{canonicalHeaders}\n{signedHeaders}\n{EmptyPayloadSha256}";
        var scope = $"{dateStamp}/{region}/s3/aws4_request";
        var stringToSign =
            $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{Sha256Hex(canonicalRequest)}";
        var signingKey = DeriveSigningKey(_settings.SecretKey, dateStamp, region, "s3");
        var signature = HmacSha256Hex(signingKey, stringToSign);
        var authorization =
            $"AWS4-HMAC-SHA256 Credential={_settings.AccessKey}/{scope}, " +
            $"SignedHeaders={signedHeaders}, Signature={signature}";
        var scheme = _settings.UseTls ? "https" : "http";

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{scheme}://{hostHeader}{canonicalUri}");
        request.Headers.Host = hostHeader;
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", EmptyPayloadSha256);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        if (!String.IsNullOrWhiteSpace(cacheControl))
            request.Headers.TryAddWithoutValidation("Cache-Control", cacheControl);

        request.Content = new ByteArrayContent(Array.Empty<byte>());
        request.Content.Headers.ContentLength = 0;
        request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

        using var response = await S3HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException(
            $"Direct zero-byte S3 PUT failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). {body}");
    }

    private static string EncodeS3Path(string value) =>
        String.Join("/", value.Split('/').Select(Uri.EscapeDataString));

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static byte[] DeriveSigningKey(string secretKey, string dateStamp, string region, string service)
    {
        var dateKey = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretKey), dateStamp);
        var regionKey = HmacSha256(dateKey, region);
        var serviceKey = HmacSha256(regionKey, service);
        return HmacSha256(serviceKey, "aws4_request");
    }

    private static byte[] HmacSha256(byte[] key, string value)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static string HmacSha256Hex(byte[] key, string value) =>
        Convert.ToHexString(HmacSha256(key, value)).ToLowerInvariant();

    private static void CommitBatch(
        MigrationRunState state,
        string containerName,
        ObjectCopyBatchResult result,
        ref int copiedThisRun,
        ref long bytesCopiedThisRun)
    {
        var count = result.Items.Count;
        var last = result.Items[^1];

        state.RecordsRead += count;
        state.RecordsWritten += count;
        state.BytesRead += result.Bytes;
        state.BytesWritten += result.Bytes;
        state.CurrentTable = containerName;
        state.HeadPartitionKey = containerName;
        state.HeadRowKey = last.Name;
        state.LastUpdatedDate = DateTime.UtcNow;

        copiedThisRun += count;
        bytesCopiedThisRun += result.Bytes;
    }

    private static void ReportProgress(
        Action<ObjectMigrationProgress>? progress,
        int copiedThisRun,
        long bytesCopiedThisRun,
        MigrationRunState state,
        TimeSpan elapsed)
    {
        progress?.Invoke(new ObjectMigrationProgress
        {
            RunObjectsWritten = copiedThisRun,
            RunBytesWritten = bytesCopiedThisRun,
            TotalObjectsWritten = state.RecordsWritten,
            TotalBytesWritten = state.BytesWritten,
            Container = state.CurrentTable ?? String.Empty,
            ObjectKey = state.HeadRowKey ?? String.Empty,
            Elapsed = elapsed
        });
    }

    private async Task EnsureBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        if (await _target.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), cancellationToken).ConfigureAwait(false))
            return;

        var create = new MakeBucketArgs().WithBucket(bucketName);
        if (!String.IsNullOrWhiteSpace(_settings.Region))
            create = create.WithLocation(_settings.Region);

        try
        {
            await _target.MakeBucketAsync(create, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (await _target.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), cancellationToken).ConfigureAwait(false))
                return;
            throw;
        }
    }

    private static bool ShouldProcessContainer(string containerName, string? checkpointContainer)
    {
        if (String.IsNullOrEmpty(checkpointContainer)) return true;
        return StringComparer.Ordinal.Compare(containerName, checkpointContainer) >= 0;
    }

    private static MigrationRunState NewState() => new()
    {
        MigrationKey = MigrationKey,
        DefinitionSha256 = DefinitionSha256,
        State = "NotStarted",
        PassNumber = 1,
        CreationDate = DateTime.UtcNow,
        LastUpdatedDate = DateTime.UtcNow
    };

    private sealed record ObjectCopyItem(string Name, long ContentLength, string? ContentType, string? CacheControl);
    private sealed record ObjectCopyResult(ObjectCopyItem Item, Exception? Error);
    private sealed record ObjectCopyBatchResult(IReadOnlyList<ObjectCopyItem> Items, long Bytes);
}