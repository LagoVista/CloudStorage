using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LagoVista.CloudStorage.Utils;
using Microsoft.Azure.Cosmos;
using Minio;
using Minio.DataModel.Args;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosArchive;

// An intentionally isolated operator utility. Never writes to or reads from operational MongoDB.
internal static class Program
{
    private static readonly JsonSerializerOptions StateJson = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 3 || !new[] { "probe", "archive", "verify", "restore", "capture-config" }.Contains(args[0], StringComparer.OrdinalIgnoreCase)
            || !new[] { "dev", "prod" }.Contains(args[1], StringComparer.OrdinalIgnoreCase)
            || String.IsNullOrWhiteSpace(args[2]))
        {
            Console.Error.WriteLine("Usage: CosmosArchive <probe|archive|verify|restore|capture-config> <dev|prod> <snapshot-id> [max-pages|new-restore-database] [page-size]");
            Console.Error.WriteLine("Example: CosmosArchive probe dev 2026-10-final 5 200");
            return 2;
        }

        var mode = args[0].ToLowerInvariant();
        var environment = args[1].ToLowerInvariant();
        var snapshot = args[2].Trim();
        if (snapshot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || snapshot.Contains('/') || snapshot.Contains('\\') || snapshot.Contains(".."))
            throw new ArgumentException("Snapshot id must be one safe path segment.");

        if (mode == "restore")
        {
            if (args.Length < 4) throw new ArgumentException("restore requires an explicit NEW destination database name");
            return await RestoreEntryAsync(environment, snapshot, args[3]);
        }

        var maxPages = args.Length > 3 ? Int32.Parse(args[3]) : 5;
        var pageSize = args.Length > 4 ? Int32.Parse(args[4]) : 200;
        if (maxPages < 0 || pageSize < 1 || pageSize > 1000)
            throw new ArgumentOutOfRangeException(nameof(args), "max-pages must be >= 0; page-size must be 1..1000. 0 pages means unlimited.");

        var source = environment == "prod" ? TestConnections.ProductionDocDB : TestConnections.DevDocDB;
        var bucket = RequiredEnv("COSMOS_ARCHIVE_SPACES_BUCKET");
        var endpoint = RequiredEnv("COSMOS_ARCHIVE_SPACES_ENDPOINT");
        var key = RequiredEnv("COSMOS_ARCHIVE_SPACES_ACCESS_KEY");
        var secret = RequiredEnv("COSMOS_ARCHIVE_SPACES_SECRET_KEY");
        var prefix = $"{snapshot}/{environment}";
        var client = new MinioClient().WithEndpoint(endpoint).WithCredentials(key, secret).WithSSL().Build();
        var cosmos = new CosmosClient(source.Uri, source.AccessKey);
        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
        var ct = shutdown.Token;
        var containers = new[] { $"{source.ResourceName}_Collections", "Devices" };
        Console.WriteLine($"Mode={mode}; environment={environment}; Cosmos database={source.ResourceName}; bucket={bucket}; prefix={prefix}; maxPages={maxPages}; pageSize={pageSize}");

        try
        {
            if (!await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct))
                throw new InvalidOperationException($"Spaces bucket '{bucket}' does not exist. Create it privately before archiving.");

            var stateKey = $"{prefix}/checkpoint.json";
            var state = await ReadStateAsync(client, bucket, stateKey, ct) ?? new ArchiveState
            {
                SnapshotId = snapshot, Environment = environment, SourceDatabase = source.ResourceName,
                Containers = containers.Select(x => new ContainerState { Name = x }).ToList()
            };
            if (state.SnapshotId != snapshot || state.Environment != environment || state.SourceDatabase != source.ResourceName ||
                !state.Containers.Select(x => x.Name).SequenceEqual(containers))
                throw new InvalidOperationException("Existing checkpoint does not match selected Cosmos source or containers.");

            if (mode == "capture-config")
            {
                var definitions = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var name in containers)
                {
                    var properties = (await cosmos.GetContainer(source.ResourceName, name)
                        .ReadContainerAsync(cancellationToken: ct)).Resource;
                    if (String.IsNullOrWhiteSpace(properties.PartitionKeyPath))
                        throw new InvalidOperationException($"Missing partition-key path for {name}");
                    definitions.Add(name, properties.PartitionKeyPath);
                    Console.WriteLine($"Captured {name}: partition key {properties.PartitionKeyPath}");
                }
                var configKey = $"{prefix}/container-config.json";
                var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(definitions, StateJson);
                using var buffer = new MemoryStream(bytes, writable: false);
                await client.PutObjectAsync(new PutObjectArgs().WithBucket(bucket).WithObject(configKey)
                    .WithStreamData(buffer).WithObjectSize(bytes.Length).WithContentType("application/json"), ct);
                Console.WriteLine($"Uploaded archived container configuration: {configKey}");
                return 0;
            }

            if (mode == "probe")
            {
                foreach (var name in containers)
                {
                    var iterator = cosmos.GetContainer(source.ResourceName, name)
                        .GetItemQueryIterator<JObject>(new QueryDefinition("SELECT * FROM c"), requestOptions: new QueryRequestOptions { MaxItemCount = pageSize });
                    var pages = 0; long records = 0;
                    while (iterator.HasMoreResults && (maxPages == 0 || pages < maxPages))
                    {
                        var page = await iterator.ReadNextAsync(ct);
                        pages++; records += page.Count;
                    }
                    Console.WriteLine($"{name}: sampled pages={pages}; documents={records}; bounded={maxPages != 0}");
                }
                return 0;
            }

            if (mode == "archive")
            {
                Console.WriteLine("Read-only Cosmos export. Type ARCHIVE to start uploading immutable gzip JSONL parts:");
                if (Console.ReadLine() != "ARCHIVE") return 2;

                foreach (var containerState in state.Containers)
                {
                    if (containerState.Completed) continue;
                    var container = cosmos.GetContainer(source.ResourceName, containerState.Name);
                    var iterator = container.GetItemQueryIterator<JObject>(
                        new QueryDefinition("SELECT * FROM c"),
                        containerState.ContinuationToken,
                        new QueryRequestOptions { MaxItemCount = pageSize });
                    var pages = 0;
                    while (iterator.HasMoreResults && (maxPages == 0 || pages < maxPages))
                    {
                        ct.ThrowIfCancellationRequested();
                        var response = await iterator.ReadNextAsync(ct);
                        var partIndex = containerState.Parts.Count;
                        var objectKey = $"{prefix}/{containerState.Name}/part-{partIndex:D8}.jsonl.gz";
                        var temp = Path.GetTempFileName();
                        try
                        {
                            using (var file = File.Create(temp))
                            using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
                            using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
                            {
                                foreach (var item in response)
                                    await writer.WriteLineAsync(item.ToString(Formatting.None));
                            }

                            var hash = await Sha256Async(temp, ct);
                            // Supply and dispose the upload stream ourselves. WithFileName can retain
                            // a Windows file handle beyond PutObjectAsync completion.
                            await using (var upload = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                await client.PutObjectAsync(new PutObjectArgs().WithBucket(bucket).WithObject(objectKey)
                                    .WithStreamData(upload).WithObjectSize(upload.Length).WithContentType("application/gzip"), ct);
                            }
                            // Verify remote bytes before committing the Cosmos continuation token.
                            var remoteHash = await RemoteSha256Async(client, bucket, objectKey, ct);
                            if (!String.Equals(hash, remoteHash, StringComparison.OrdinalIgnoreCase))
                                throw new IOException($"Remote SHA-256 mismatch for {objectKey}");

                            containerState.Parts.Add(new PartInfo
                            {
                                Key = objectKey, Sha256 = hash, Records = response.Count,
                                CompressedBytes = new FileInfo(temp).Length
                            });
                            containerState.ContinuationToken = response.ContinuationToken;
                            containerState.Completed = !iterator.HasMoreResults;
                            await WriteStateAsync(client, bucket, stateKey, state, ct);
                            pages++;
                            Console.WriteLine($"{containerState.Name}: part={partIndex:D8}; records={response.Count}; total={containerState.Parts.Sum(x => x.Records)}; completed={containerState.Completed}");
                        }
                        finally
                        {
                            // A temporary-file cleanup problem must never invalidate an already
                            // verified upload and committed continuation checkpoint.
                            try { File.Delete(temp); }
                            catch (IOException ex) { Console.Error.WriteLine($"Temporary file cleanup deferred: {temp}; {ex.Message}"); }
                            catch (UnauthorizedAccessException ex) { Console.Error.WriteLine($"Temporary file cleanup deferred: {temp}; {ex.Message}"); }
                        }
                    }
                }
                if (state.Containers.All(x => x.Completed))
                {
                    state.CompletedUtc ??= DateTime.UtcNow;
                    await WriteStateAsync(client, bucket, stateKey, state, ct);
                    await WriteStateAsync(client, bucket, $"{prefix}/manifest.json", state, ct);
                    Console.WriteLine("All containers exported; manifest published.");
                }
                else Console.WriteLine("Bounded export paused. Rerun archive with the same snapshot id to resume.");
                return 0;
            }

            if (state.CompletedUtc == null || !state.Containers.All(x => x.Completed))
                throw new InvalidOperationException("Archive is incomplete; cannot verify final snapshot.");
            foreach (var collection in state.Containers)
            {
                long count = 0;
                foreach (var part in collection.Parts)
                {
                    var hash = await RemoteSha256Async(client, bucket, part.Key, ct);
                    if (!hash.Equals(part.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new IOException($"Checksum mismatch: {part.Key}");
                    count += part.Records;
                }
                Console.WriteLine($"Verified {collection.Name}: parts={collection.Parts.Count}; recorded documents={count}");
            }
            Console.WriteLine("All compressed objects match recorded hashes. A full sample restore into a disposable target is still required before deleting Cosmos.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{mode} failed: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RestoreEntryAsync(string environment, string snapshot, string database)
    {
        try { await ArchiveRestore.RunAsync(environment, snapshot, database, CancellationToken.None); return 0; }
        catch (Exception ex) { Console.Error.WriteLine($"Restore failed: {ex.Message}"); return 1; }
    }

    private static string RequiredEnv(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException($"Missing environment variable: {name}");

    private static async Task<string> Sha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
    }

    private static async Task<string> RemoteSha256Async(IMinioClient client, string bucket, string key, CancellationToken ct)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await client.GetObjectAsync(new GetObjectArgs().WithBucket(bucket).WithObject(key)
            .WithCallbackStream(async (stream, token) =>
            {
                var buffer = new byte[65536];
                int n;
                while ((n = await stream.ReadAsync(buffer, token)) > 0)
                    hash.AppendData(buffer, 0, n);
            }), ct);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static async Task<ArchiveState?> ReadStateAsync(IMinioClient client, string bucket, string key, CancellationToken ct)
    {
        try
        {
            using var memory = new MemoryStream();
            await client.GetObjectAsync(new GetObjectArgs().WithBucket(bucket).WithObject(key)
                .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(memory, token)), ct);
            return System.Text.Json.JsonSerializer.Deserialize<ArchiveState>(memory.ToArray(), StateJson);
        }
        catch (Minio.Exceptions.ObjectNotFoundException) { return null; }
    }

    private static async Task WriteStateAsync(IMinioClient client, string bucket, string key, ArchiveState state, CancellationToken ct)
    {
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(state, StateJson);
        using var memory = new MemoryStream(bytes, writable: false);
        await client.PutObjectAsync(new PutObjectArgs().WithBucket(bucket).WithObject(key)
            .WithStreamData(memory).WithObjectSize(bytes.LongLength).WithContentType("application/json"), ct);
    }
}

internal sealed class ArchiveState
{
    public string SnapshotId { get; set; } = "";
    public string Environment { get; set; } = "";
    public string SourceDatabase { get; set; } = "";
    public DateTime? CompletedUtc { get; set; }
    public List<ContainerState> Containers { get; set; } = new();
}

internal sealed class ContainerState
{
    public string Name { get; set; } = "";
    public string? ContinuationToken { get; set; }
    public bool Completed { get; set; }
    public List<PartInfo> Parts { get; set; } = new();
}

internal sealed class PartInfo
{
    public string Key { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Records { get; set; }
    public long CompressedBytes { get; set; }
}
