using Azure.Storage.Blobs;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using Minio;
using Minio.DataModel.Args;
using System.Diagnostics;

namespace LagoVista.StorageMigration;

public sealed class ObjectStorageVerificationResult
{
    public long AzureObjectCount { get; init; }
    public long SeaweedObjectCount { get; init; }
    public long AzureBytes { get; init; }
    public long SeaweedBytes { get; init; }
    public IReadOnlyList<string> MissingObjects { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> UnexpectedObjects { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ObjectStorageSizeMismatch> SizeMismatches { get; init; } = Array.Empty<ObjectStorageSizeMismatch>();
    public bool Matches => MissingObjects.Count == 0 && UnexpectedObjects.Count == 0 && SizeMismatches.Count == 0;
}

public sealed record ObjectStorageSizeMismatch(string Key, long AzureBytes, long SeaweedBytes);

public sealed class AzureBlobToS3Verifier
{
    private const int ProgressObjectInterval = 5_000;
    private static readonly TimeSpan ProgressTimeInterval = TimeSpan.FromSeconds(5);

    private readonly BlobServiceClient _azure;
    private readonly IMinioClient _s3;

    public AzureBlobToS3Verifier(string azureConnectionString, IS3ObjectStorageConnectionSettings settings)
    {
        _azure = new BlobServiceClient(azureConnectionString);

        var builder = new MinioClient()
            .WithEndpoint(settings.Host, settings.Port)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseTls);

        if (!String.IsNullOrWhiteSpace(settings.Region))
            builder = builder.WithRegion(settings.Region);

        _s3 = builder.Build();
    }

    public async Task<ObjectStorageVerificationResult> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastProgress = TimeSpan.Zero;

        Console.WriteLine("[1/3] Enumerating Azure Blob objects...");
        var azureObjects = new Dictionary<string, long>(StringComparer.Ordinal);
        await foreach (var container in _azure.GetBlobContainersAsync(cancellationToken: cancellationToken))
        {
            var containerClient = _azure.GetBlobContainerClient(container.Name);
            await foreach (var blob in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                azureObjects[BuildKey(container.Name, blob.Name)] = blob.Properties.ContentLength ?? 0;
                ReportProgress("Azure", azureObjects.Count, stopwatch, ref lastProgress);
            }
        }
        Console.WriteLine($"      Azure complete: {azureObjects.Count:N0} objects, {azureObjects.Values.Sum():N0} bytes.");

        Console.WriteLine("[2/3] Enumerating SeaweedFS S3 objects...");
        lastProgress = stopwatch.Elapsed;
        var seaweedObjects = new Dictionary<string, long>(StringComparer.Ordinal);
        var buckets = await _s3.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var bucket in buckets.Buckets)
        {
            var args = new ListObjectsArgs()
                .WithBucket(bucket.Name)
                .WithRecursive(true);

            await foreach (var item in _s3.ListObjectsEnumAsync(args, cancellationToken).ConfigureAwait(false))
            {
                seaweedObjects[BuildKey(bucket.Name, item.Key)] = checked((long)item.Size);
                ReportProgress("SeaweedFS", seaweedObjects.Count, stopwatch, ref lastProgress);
            }
        }
        Console.WriteLine($"      SeaweedFS complete: {seaweedObjects.Count:N0} objects, {seaweedObjects.Values.Sum():N0} bytes.");

        Console.WriteLine("[3/3] Comparing object keys and sizes...");

        var missing = azureObjects.Keys
            .Where(key => !seaweedObjects.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var unexpected = seaweedObjects.Keys
            .Where(key => !azureObjects.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var mismatches = azureObjects
            .Where(pair => seaweedObjects.TryGetValue(pair.Key, out var targetSize) && targetSize != pair.Value)
            .Select(pair => new ObjectStorageSizeMismatch(pair.Key, pair.Value, seaweedObjects[pair.Key]))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();

        Console.WriteLine($"      Comparison complete in {stopwatch.Elapsed.TotalSeconds:0.0}s.");

        return new ObjectStorageVerificationResult
        {
            AzureObjectCount = azureObjects.Count,
            SeaweedObjectCount = seaweedObjects.Count,
            AzureBytes = azureObjects.Values.Sum(),
            SeaweedBytes = seaweedObjects.Values.Sum(),
            MissingObjects = missing,
            UnexpectedObjects = unexpected,
            SizeMismatches = mismatches
        };
    }

    private static void ReportProgress(string phase, int objectCount, Stopwatch stopwatch, ref TimeSpan lastProgress)
    {
        var elapsedSinceProgress = stopwatch.Elapsed - lastProgress;
        if (objectCount % ProgressObjectInterval != 0 && elapsedSinceProgress < ProgressTimeInterval)
            return;

        Console.WriteLine($"      {phase}: {objectCount:N0} objects scanned ({stopwatch.Elapsed.TotalSeconds:0.0}s elapsed)");
        lastProgress = stopwatch.Elapsed;
    }

    private static string BuildKey(string container, string objectKey) => $"{container}/{objectKey}";
}
