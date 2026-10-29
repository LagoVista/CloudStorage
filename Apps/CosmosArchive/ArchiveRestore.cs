using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LagoVista.CloudStorage.Utils;
using Microsoft.Azure.Cosmos;
using Minio;
using Minio.DataModel.Args;
using Newtonsoft.Json.Linq;

namespace CosmosArchive;

internal static class ArchiveRestore
{
    // Deliberately create-only: never overwrite source or an existing restore database.
    public static async Task RunAsync(string environment, string snapshot, string destinationDatabase, CancellationToken ct)
    {
        var source = environment == "prod" ? TestConnections.ProductionDocDB : TestConnections.DevDocDB;
        if (String.IsNullOrWhiteSpace(destinationDatabase) || destinationDatabase.Equals(source.ResourceName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Restore destination must be a new database, different from the source.");
        var bucket = Required("COSMOS_ARCHIVE_SPACES_BUCKET");
        var minio = new MinioClient().WithEndpoint(Required("COSMOS_ARCHIVE_SPACES_ENDPOINT"))
            .WithCredentials(Required("COSMOS_ARCHIVE_SPACES_ACCESS_KEY"), Required("COSMOS_ARCHIVE_SPACES_SECRET_KEY")).WithSSL().Build();
        var prefix = $"{snapshot}/{environment}";
        byte[] manifestBytes;
        using (var mem = new MemoryStream())
        {
            await minio.GetObjectAsync(new GetObjectArgs().WithBucket(bucket).WithObject($"{prefix}/manifest.json")
                .WithCallbackStream(async (s, token) => await s.CopyToAsync(mem, token)), ct);
            manifestBytes = mem.ToArray();
        }
        var manifest = System.Text.Json.JsonSerializer.Deserialize<ArchiveState>(manifestBytes,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Invalid manifest.");
        if (manifest.Environment != environment || manifest.SourceDatabase != source.ResourceName || manifest.SnapshotId != snapshot
            || manifest.CompletedUtc == null || manifest.Containers.Any(x => !x.Completed))
            throw new InvalidOperationException("Manifest does not represent a complete matching archive.");

        using var cosmos = new CosmosClient(source.Uri, source.AccessKey);
        var sourceDb = cosmos.GetDatabase(source.ResourceName);
        // To test recovery without creating another paid Cosmos account, restore to a new database in the same account.
        // A missing database is required: an accidental rerun cannot overwrite a recovered database.
        try
        {
            await cosmos.GetDatabase(destinationDatabase).ReadAsync(cancellationToken: ct);
            throw new InvalidOperationException($"Refusing restore: destination database '{destinationDatabase}' already exists.");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }

        Console.WriteLine($"Restoring archive {snapshot}/{environment} into NEW Cosmos database {destinationDatabase}. Type RESTORE:");
        if (Console.ReadLine() != "RESTORE") throw new OperationCanceledException("Restore not confirmed.");
        var destination = (await cosmos.CreateDatabaseAsync(destinationDatabase, cancellationToken: ct)).Database;
        foreach (var sourceContainer in manifest.Containers)
        {
            var definition = (await sourceDb.GetContainer(sourceContainer.Name).ReadContainerAsync(cancellationToken: ct)).Resource;
            // Create from source partition key, but do not copy throughput or policies tied to billing.
            var partitionPath = definition.PartitionKeyPath;
            if (String.IsNullOrWhiteSpace(partitionPath))
                throw new InvalidOperationException($"Could not resolve partition key for {sourceContainer.Name}");
            var targetContainer = (await destination.CreateContainerAsync(
                new ContainerProperties(sourceContainer.Name, partitionPath), cancellationToken: ct)).Container;
            long restored = 0;
            foreach (var part in sourceContainer.Parts)
            {
                using var compressed = new MemoryStream();
                await minio.GetObjectAsync(new GetObjectArgs().WithBucket(bucket).WithObject(part.Key)
                    .WithCallbackStream(async (s, token) => await s.CopyToAsync(compressed, token)), ct);
                var actualHash = Convert.ToHexString(SHA256.HashData(compressed.ToArray()));
                if (!actualHash.Equals(part.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Archive SHA-256 mismatch: {part.Key}");
                compressed.Position = 0;
                using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Encoding.UTF8);
                long partRestored = 0;
                string? line;
                while ((line = await reader.ReadLineAsync(ct)) != null)
                {
                    var doc = JObject.Parse(line);
                    // Cosmos-managed properties cannot be supplied on a fresh insert.
                    foreach (var name in new[] { "_rid", "_self", "_etag", "_attachments", "_ts" }) doc.Remove(name);
                    await targetContainer.CreateItemAsync(doc, cancellationToken: ct);
                    partRestored++;
                }
                if (partRestored != part.Records)
                    throw new InvalidOperationException($"Part {part.Key} had {partRestored} records, manifest expected {part.Records}.");
                restored += partRestored;
            }
            Console.WriteLine($"{sourceContainer.Name}: restored {restored} documents (expected {sourceContainer.Parts.Sum(x=>x.Records)})");
            if (restored != sourceContainer.Parts.Sum(x => x.Records))
                throw new InvalidOperationException("Restored counts differ from archive.");
        }
        Console.WriteLine($"Restore completed to {destinationDatabase}. Inspect representative records before approval.");
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } v
        ? v : throw new InvalidOperationException($"Missing {name}");
}
