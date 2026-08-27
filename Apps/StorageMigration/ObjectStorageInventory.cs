using Azure.Storage.Blobs;
using Minio;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;

namespace LagoVista.StorageMigration;

public sealed class ObjectStorageContainerInventory
{
    public string Container { get; set; } = String.Empty;
    public long ObjectCount { get; set; }
    public long TotalBytes { get; set; }
    public DateTimeOffset? OldestLastModified { get; set; }
    public DateTimeOffset? NewestLastModified { get; set; }
}

public sealed class ObjectStorageInventoryResult
{
    public List<ObjectStorageContainerInventory> Containers { get; } = new();
    public long ObjectCount => Containers.Sum(item => item.ObjectCount);
    public long TotalBytes => Containers.Sum(item => item.TotalBytes);
    public bool WasLimited { get; set; }
}

public sealed class AzureBlobObjectInventorySource
{
    private readonly BlobServiceClient _client;

    public AzureBlobObjectInventorySource(string connectionString)
    {
        _client = new BlobServiceClient(connectionString);
    }

    public async Task<ObjectStorageInventoryResult> InventoryAsync(int? maxObjects = null, CancellationToken cancellationToken = default)
    {
        var result = new ObjectStorageInventoryResult();
        var objectsSeen = 0L;

        await foreach (var container in _client.GetBlobContainersAsync(cancellationToken: cancellationToken))
        {
            var summary = new ObjectStorageContainerInventory { Container = container.Name };
            var containerClient = _client.GetBlobContainerClient(container.Name);

            await foreach (var blob in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                summary.ObjectCount++;
                summary.TotalBytes += blob.Properties.ContentLength ?? 0;

                if (blob.Properties.LastModified.HasValue)
                {
                    var lastModified = blob.Properties.LastModified.Value;
                    if (!summary.OldestLastModified.HasValue || lastModified < summary.OldestLastModified.Value)
                        summary.OldestLastModified = lastModified;
                    if (!summary.NewestLastModified.HasValue || lastModified > summary.NewestLastModified.Value)
                        summary.NewestLastModified = lastModified;
                }

                objectsSeen++;
                if (maxObjects.HasValue && objectsSeen >= maxObjects.Value)
                {
                    result.WasLimited = true;
                    break;
                }
            }

            result.Containers.Add(summary);
            if (result.WasLimited) break;
        }

        return result;
    }
}

public sealed class S3ObjectStorageProbe
{
    private readonly IMinioClient _client;

    public S3ObjectStorageProbe(IS3ObjectStorageConnectionSettings settings)
    {
        var builder = new MinioClient()
            .WithEndpoint(settings.Host, settings.Port)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseTls);

        if (!String.IsNullOrWhiteSpace(settings.Region))
            builder = builder.WithRegion(settings.Region);

        _client = builder.Build();
    }

    public async Task<IReadOnlyList<string>> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _client.ListBucketsAsync(cancellationToken);
        return result.Buckets.Select(bucket => bucket.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
