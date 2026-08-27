using Azure.Storage.Blobs;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using Minio;
using Minio.DataModel.Args;
using System.Security.Cryptography;
using System.Text;

namespace LagoVista.StorageMigration;

public sealed class AzureBlobToS3Migration
{
    public const string MigrationKey = "azure-blob-to-s3";
    private static readonly string DefinitionSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("azure-blob-to-s3-v1")));

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

    public async Task<MigrationRunState> ExecuteAsync(int? maxObjects = null, CancellationToken cancellationToken = default)
    {
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

        foreach (var containerName in containers)
        {
            if (!ShouldProcessContainer(containerName, state.CurrentTable))
                continue;

            await EnsureBucketAsync(containerName, cancellationToken).ConfigureAwait(false);
            var container = _source.GetBlobContainerClient(containerName);
            var resumingCurrentContainer = String.Equals(containerName, state.CurrentTable, StringComparison.Ordinal);
            var sawBlob = false;

            await foreach (var blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                sawBlob = true;

                if (resumingCurrentContainer && !String.IsNullOrEmpty(state.HeadRowKey) &&
                    StringComparer.Ordinal.Compare(blob.Name, state.HeadRowKey) <= 0)
                    continue;

                if (maxObjects.HasValue && copiedThisRun >= maxObjects.Value)
                {
                    state.State = "Paused";
                    await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
                    return state;
                }

                var contentLength = blob.Properties.ContentLength ?? 0;
                state.RecordsRead++;
                state.BytesRead += contentLength;

                try
                {
                    var sourceBlob = container.GetBlobClient(blob.Name);
                    var download = await sourceBlob.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    await using var stream = download.Value.Content;

                    var put = new PutObjectArgs()
                        .WithBucket(containerName)
                        .WithObject(blob.Name)
                        .WithStreamData(stream)
                        .WithObjectSize(contentLength)
                        .WithContentType(String.IsNullOrWhiteSpace(blob.Properties.ContentType)
                            ? "application/octet-stream"
                            : blob.Properties.ContentType);

                    if (!String.IsNullOrWhiteSpace(blob.Properties.CacheControl))
                    {
                        put = put.WithHeaders(new Dictionary<string, string>
                        {
                            ["Cache-Control"] = blob.Properties.CacheControl
                        });
                    }

                    await _target.PutObjectAsync(put, cancellationToken).ConfigureAwait(false);

                    state.RecordsWritten++;
                    state.BytesWritten += contentLength;
                    state.CurrentTable = containerName;
                    state.HeadPartitionKey = containerName;
                    state.HeadRowKey = blob.Name;
                    state.LastUpdatedDate = DateTime.UtcNow;
                    copiedThisRun++;

                    await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    state.RecordsFailed++;
                    state.State = "Failed";
                    state.LastUpdatedDate = DateTime.UtcNow;
                    await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
                    throw;
                }
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
        return state;
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
}
