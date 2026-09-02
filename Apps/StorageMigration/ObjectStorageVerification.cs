using Azure.Storage.Blobs;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using Minio;
using Minio.DataModel.Args;

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
        var azureObjects = new Dictionary<string, long>(StringComparer.Ordinal);
        await foreach (var container in _azure.GetBlobContainersAsync(cancellationToken: cancellationToken))
        {
            var containerClient = _azure.GetBlobContainerClient(container.Name);
            await foreach (var blob in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
                azureObjects[BuildKey(container.Name, blob.Name)] = blob.Properties.ContentLength ?? 0;
        }

        var seaweedObjects = new Dictionary<string, long>(StringComparer.Ordinal);
        var buckets = await _s3.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var bucket in buckets.Buckets)
        {
            var args = new ListObjectsArgs()
                .WithBucket(bucket.Name)
                .WithRecursive(true);

            await foreach (var item in _s3.ListObjectsEnumAsync(args, cancellationToken).ConfigureAwait(false))
                seaweedObjects[BuildKey(bucket.Name, item.Key)] = checked((long)item.Size);
        }

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

    private static string BuildKey(string container, string objectKey) => $"{container}/{objectKey}";
}
