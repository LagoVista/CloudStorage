using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EntityMigration202608;

internal sealed class EntityMigrationRunner
{
    private static readonly string[] DestinationCollections =
    {
        "Entities",
        "MediaResources",
        "Devices"
    };

    private static readonly HashSet<string> ExcludedEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    private readonly string _environment;
    private readonly IDocumentMigrationService _migrationService;
    private readonly DocumentStorageSettings _source;
    private readonly MongoDocumentStorageSettings _target;
    private readonly MigrationProgressStore _progressStore;

    public EntityMigrationRunner(string environment)
    {
        _environment = String.Equals(environment, "prod", StringComparison.OrdinalIgnoreCase) ? "prod" : "dev";

        var sourceConnection = _environment == "prod"
            ? TestConnections.ProductionDocDB
            : TestConnections.DevDocDB;

        _source = new DocumentStorageSettings
        {
            Provider = DocumentStorageProviderType.Cosmos,
            Endpoint = sourceConnection.Uri,
            SharedKey = sourceConnection.AccessKey,
            DatabaseName = sourceConnection.ResourceName
        };

        var mongoConnection = _environment == "prod"
            ? TestConnections.ProductionMongoDocumentStorage
            : TestConnections.DevMongoDocumentStorage;

        _target = new MongoDocumentStorageSettings
        {
            ConnectionString = mongoConnection.BuildConnectionString(),
            DatabaseName = String.IsNullOrWhiteSpace(mongoConnection.DatabaseName)
                ? _source.DatabaseName
                : mongoConnection.DatabaseName
        };

        _migrationService = new DocumentMigrationService(CosmosClientProvider.Shared, new DocumentCollectionNameResolver());
        _progressStore = MigrationProgressStore.Create(_environment);
    }

    public async Task DryRunAsync(int batchSize = 200, int maxPages = 0, CancellationToken ct = default)
    {
        PrintPlan("DRY RUN", batchSize, maxPages);

        foreach (var sourceCollectionName in GetSourceCollections())
        {
            Console.WriteLine();
            Console.WriteLine($"Source collection: {sourceCollectionName}");
            var request = CreateRequest(sourceCollectionName, true, batchSize, maxPages, null);
            var result = await _migrationService.MigrateCosmosToMongoAsync(request, ct).ConfigureAwait(false);
            PrintMigrationResult(result);
        }
    }

    public async Task MigrateAsync(int batchSize = 200, int maxPages = 0, CancellationToken ct = default)
    {
        var sourceCollections = GetSourceCollections().ToArray();
        var progress = await _progressStore.LoadOrCreateAsync(
            _source.DatabaseName,
            _target.DatabaseName,
            sourceCollections,
            ct).ConfigureAwait(false);

        PrintPlan("WRITE", batchSize, maxPages);
        PrintProgress(progress);

        if (progress.Sources.All(source => source.Completed))
        {
            Console.WriteLine();
            Console.WriteLine("Migration is already complete. Use reset if you intentionally want to start over.");
            return;
        }

        Console.WriteLine();
        Console.Write("Type MIGRATE to copy eligible entity documents to Mongo and advance the saved checkpoint: ");
        if (!String.Equals(Console.ReadLine(), "MIGRATE", StringComparison.Ordinal))
        {
            Console.WriteLine("Migration cancelled.");
            return;
        }

        var run = new EntityMigrationRunSummary
        {
            RunId = Guid.NewGuid().ToString("N"),
            Operation = "migrate",
            StartedUtc = DateTime.UtcNow,
            Status = "Running",
            BatchSize = batchSize,
            MaxPagesPerSource = maxPages
        };

        progress.RunCount++;
        progress.Status = "InProgress";
        progress.CompletedUtc = null;
        progress.Runs ??= new List<EntityMigrationRunSummary>();
        progress.Runs.Add(run);
        TrimRunHistory(progress);
        await _progressStore.SaveAsync(progress, ct).ConfigureAwait(false);

        try
        {
            foreach (var sourceCollectionName in sourceCollections)
            {
                var sourceProgress = progress.Sources.Single(source =>
                    String.Equals(source.SourceCollectionName, sourceCollectionName, StringComparison.OrdinalIgnoreCase));

                if (sourceProgress.Completed)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Source collection: {sourceCollectionName} [already complete]");
                    continue;
                }

                Console.WriteLine();
                Console.WriteLine($"Source collection: {sourceCollectionName}");

                var pagesThisRun = 0;
                while (!sourceProgress.Completed && (maxPages <= 0 || pagesThisRun < maxPages))
                {
                    ct.ThrowIfCancellationRequested();

                    var request = CreateRequest(
                        sourceCollectionName,
                        false,
                        batchSize,
                        1,
                        sourceProgress.ContinuationToken);

                    var result = await _migrationService.MigrateCosmosToMongoAsync(request, ct).ConfigureAwait(false);
                    ApplyPage(progress, sourceProgress, run, result);
                    pagesThisRun += result.PagesRead;

                    await _progressStore.SaveAsync(progress, ct).ConfigureAwait(false);
                    PrintPageCheckpoint(sourceProgress, result);

                    if (result.PagesRead == 0)
                        break;
                }
            }

            var completed = progress.Sources.All(source => source.Completed);
            progress.Status = completed ? "Completed" : "Paused";
            progress.CompletedUtc = completed ? DateTime.UtcNow : null;
            run.Status = completed ? "Completed" : "Paused";
            run.FinishedUtc = DateTime.UtcNow;
            await _progressStore.SaveAsync(progress, ct).ConfigureAwait(false);

            Console.WriteLine();
            PrintProgress(progress);
        }
        catch (OperationCanceledException)
        {
            progress.Status = "Paused";
            run.Status = "Cancelled";
            run.FinishedUtc = DateTime.UtcNow;
            await TrySaveProgressAsync(progress).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            progress.Status = "Failed";
            run.Status = "Failed";
            run.Error = ex.Message;
            run.FinishedUtc = DateTime.UtcNow;
            await TrySaveProgressAsync(progress).ConfigureAwait(false);
            throw;
        }
    }

    public async Task ShowStatusAsync(CancellationToken ct = default)
    {
        var progress = await _progressStore.GetAsync(ct).ConfigureAwait(false);
        if (progress == null)
        {
            Console.WriteLine($"No saved EntityMigration202608 progress exists for {_environment}.");
            Console.WriteLine($"Application Data database: {_progressStore.DatabaseName}");
            return;
        }

        PrintProgress(progress);

        if (progress.Runs?.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Recent runs:");
            Console.WriteLine($"{"Started UTC",-22} {"Status",-12} {"Pages",8} {"Read",10} {"Written",10} {"Failed",8}");
            foreach (var run in progress.Runs.OrderByDescending(item => item.StartedUtc).Take(10))
                Console.WriteLine($"{run.StartedUtc:u} {run.Status,-12} {run.PagesRead,8} {run.DocumentsRead,10} {run.DocumentsWritten,10} {run.DocumentsFailed,8}");
        }
    }

    public async Task ValidateAsync(int batchSize = 200, CancellationToken ct = default)
    {
        PrintPlan("VALIDATE", batchSize, 0);

        var sourceRoutes = new Dictionary<(string CollectionName, string EntityType), long>();

        foreach (var sourceCollectionName in GetSourceCollections())
        {
            Console.WriteLine();
            Console.WriteLine($"Inventorying source collection: {sourceCollectionName}");

            var request = CreateRequest(sourceCollectionName, true, batchSize, 0, null);
            var inventory = await _migrationService.MigrateCosmosToMongoAsync(request, ct).ConfigureAwait(false);

            foreach (var route in inventory.Routes)
            {
                var key = (route.CollectionName ?? String.Empty, route.EntityType ?? String.Empty);
                var eligibleCount = route.Read - route.Excluded;
                sourceRoutes[key] = sourceRoutes.TryGetValue(key, out var existing)
                    ? existing + eligibleCount
                    : eligibleCount;
            }
        }

        var client = new MongoClient(_target.ConnectionString);
        var database = client.GetDatabase(_target.DatabaseName);
        var validationRoutes = new List<DocumentMigrationValidationStatistics>();

        foreach (var sourceRoute in sourceRoutes.OrderBy(item => item.Key.CollectionName, StringComparer.OrdinalIgnoreCase)
                                                .ThenBy(item => item.Key.EntityType, StringComparer.OrdinalIgnoreCase))
        {
            var collection = database.GetCollection<BsonDocument>(sourceRoute.Key.CollectionName);
            var destinationCount = await collection.CountDocumentsAsync(
                CreateMongoEntityTypeFilter(sourceRoute.Key.EntityType),
                cancellationToken: ct).ConfigureAwait(false);

            validationRoutes.Add(new DocumentMigrationValidationStatistics
            {
                EntityType = sourceRoute.Key.EntityType,
                CollectionName = sourceRoute.Key.CollectionName,
                SourceCount = sourceRoute.Value,
                DestinationCount = destinationCount
            });
        }

        var sourceCount = validationRoutes.Sum(item => item.SourceCount);
        var destinationCountTotal = validationRoutes.Sum(item => item.DestinationCount);
        var matches = validationRoutes.All(item => item.Matches);

        Console.WriteLine();
        Console.WriteLine($"Source eligible:      {sourceCount}");
        Console.WriteLine($"Mongo destination:    {destinationCountTotal}");
        Console.WriteLine($"Matches:              {matches}");
        Console.WriteLine();
        Console.WriteLine($"{"Collection",-20} {"EntityType",-40} {"Source",10} {"Mongo",10} {"Match",7}");

        foreach (var route in validationRoutes)
            Console.WriteLine($"{route.CollectionName,-20} {DisplayEntityType(route.EntityType),-40} {route.SourceCount,10} {route.DestinationCount,10} {(route.Matches ? "yes" : "NO"),7}");
    }

    public async Task ResetMongoAsync(CancellationToken ct = default)
    {
        PrintTarget();
        Console.WriteLine();
        Console.WriteLine($"This will permanently drop Mongo collections {String.Join(", ", DestinationCollections)} and delete the saved migration checkpoint.");
        Console.Write("Type RESET to continue: ");
        if (!String.Equals(Console.ReadLine(), "RESET", StringComparison.Ordinal))
        {
            Console.WriteLine("Reset cancelled.");
            return;
        }

        var client = new MongoClient(_target.ConnectionString);
        var database = client.GetDatabase(_target.DatabaseName);
        var existingCollections = await database.ListCollectionNames().ToListAsync(ct).ConfigureAwait(false);

        foreach (var collectionName in DestinationCollections)
        {
            if (!existingCollections.Contains(collectionName, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{collectionName}: not present.");
                continue;
            }

            await database.DropCollectionAsync(collectionName, ct).ConfigureAwait(false);
            Console.WriteLine($"Dropped {_target.DatabaseName}.{collectionName}.");
        }

        await _progressStore.DeleteAsync(ct).ConfigureAwait(false);
        Console.WriteLine("Deleted saved EntityMigration202608 progress.");
    }

    private IEnumerable<string> GetSourceCollections()
    {
        yield return $"{_source.DatabaseName}_Collections";
        yield return "Devices";
    }

    private CosmosToMongoMigrationRequest CreateRequest(string sourceCollectionName, bool dryRun, int batchSize, int maxPages, string continuationToken)
    {
        return new CosmosToMongoMigrationRequest
        {
            Source = _source,
            Target = _target,
            SourceCollectionName = sourceCollectionName,
            BatchSize = batchSize,
            MaxPages = maxPages,
            ContinuationToken = continuationToken,
            DryRun = dryRun,
            ExcludedEntityTypes = ExcludedEntityTypes.ToArray()
        };
    }

    private void PrintPlan(string operation, int batchSize, int maxPages)
    {
        Console.WriteLine($"August 2026 entity migration - {operation}");
        Console.WriteLine($"Environment:          {_environment}");
        Console.WriteLine($"Cosmos database:      {_source.DatabaseName}");
        Console.WriteLine($"Cosmos collections:   {String.Join(", ", GetSourceCollections())}");
        Console.WriteLine($"Mongo database:       {_target.DatabaseName}");
        Console.WriteLine($"Mongo collections:    {String.Join(", ", DestinationCollections)}");
        Console.WriteLine($"Application Data DB:  {_progressStore.DatabaseName}");
        Console.WriteLine($"Batch size:           {batchSize}");
        Console.WriteLine($"Max pages/source:     {(maxPages <= 0 ? "all" : maxPages)}");
        Console.WriteLine($"Excluded EntityTypes: {(ExcludedEntityTypes.Count == 0 ? "<none>" : String.Join(", ", ExcludedEntityTypes.OrderBy(item => item)))}");
    }

    private void PrintTarget()
    {
        Console.WriteLine("August 2026 entity migration reset");
        Console.WriteLine($"Environment:          {_environment}");
        Console.WriteLine($"Mongo database:       {_target.DatabaseName}");
        Console.WriteLine($"Mongo collections:    {String.Join(", ", DestinationCollections)}");
        Console.WriteLine($"Application Data DB:  {_progressStore.DatabaseName}");
    }

    private static void ApplyPage(
        EntityMigration202608Progress progress,
        EntityMigrationSourceProgress source,
        EntityMigrationRunSummary run,
        CosmosToMongoMigrationResult result)
    {
        source.PagesRead += result.PagesRead;
        source.DocumentsRead += result.DocumentsRead;
        source.DocumentsWritten += result.DocumentsWritten;
        source.DocumentsExcluded += result.DocumentsExcluded;
        source.DocumentsSkipped += result.DocumentsSkipped;
        source.DocumentsFailed += result.DocumentsFailed;
        source.ContinuationToken = result.ContinuationToken;
        source.Completed = result.Completed;

        run.PagesRead += result.PagesRead;
        run.DocumentsRead += result.DocumentsRead;
        run.DocumentsWritten += result.DocumentsWritten;
        run.DocumentsFailed += result.DocumentsFailed;

        progress.Status = "InProgress";
    }

    private static void PrintPageCheckpoint(EntityMigrationSourceProgress source, CosmosToMongoMigrationResult result)
    {
        Console.WriteLine(
            $"  checkpoint: page +{result.PagesRead}, read +{result.DocumentsRead}, written +{result.DocumentsWritten}, " +
            $"total pages={source.PagesRead}, total read={source.DocumentsRead}, complete={source.Completed}");
    }

    private void PrintProgress(EntityMigration202608Progress progress)
    {
        Console.WriteLine();
        Console.WriteLine("Saved migration progress");
        Console.WriteLine($"Status:               {progress.Status}");
        Console.WriteLine($"Runs:                 {progress.RunCount}");
        Console.WriteLine($"Source database:      {progress.SourceDatabaseName}");
        Console.WriteLine($"Target database:      {progress.TargetDatabaseName}");
        Console.WriteLine($"Last updated:         {progress.LastUpdatedDate}");
        Console.WriteLine($"Application Data DB:  {_progressStore.DatabaseName}");
        Console.WriteLine();
        Console.WriteLine($"{"Source Collection",-36} {"Done",6} {"Pages",8} {"Read",10} {"Written",10} {"Failed",8} {"Token",8}");

        foreach (var source in progress.Sources)
        {
            Console.WriteLine(
                $"{source.SourceCollectionName,-36} {(source.Completed ? "yes" : "no"),6} {source.PagesRead,8} {source.DocumentsRead,10} " +
                $"{source.DocumentsWritten,10} {source.DocumentsFailed,8} {(String.IsNullOrWhiteSpace(source.ContinuationToken) ? "<none>" : "saved"),8}");
        }
    }

    private async Task TrySaveProgressAsync(EntityMigration202608Progress progress)
    {
        try
        {
            await _progressStore.SaveAsync(progress, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to save final migration checkpoint: {ex.Message}");
        }
    }

    private static void TrimRunHistory(EntityMigration202608Progress progress)
    {
        const int maxRunHistory = 25;
        if (progress.Runs.Count <= maxRunHistory) return;
        progress.Runs = progress.Runs.OrderByDescending(run => run.StartedUtc).Take(maxRunHistory).OrderBy(run => run.StartedUtc).ToList();
    }

    private static FilterDefinition<BsonDocument> CreateMongoEntityTypeFilter(string entityType)
    {
        if (!String.IsNullOrWhiteSpace(entityType))
            return Builders<BsonDocument>.Filter.Eq("EntityType", entityType);

        return Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("EntityType", false),
            Builders<BsonDocument>.Filter.Eq("EntityType", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("EntityType", String.Empty));
    }

    private static void PrintMigrationResult(CosmosToMongoMigrationResult result)
    {
        Console.WriteLine();
        Console.WriteLine($"Pages read:           {result.PagesRead}");
        Console.WriteLine($"Documents read:       {result.DocumentsRead}");
        Console.WriteLine($"Documents written:    {result.DocumentsWritten}");
        Console.WriteLine($"Documents excluded:   {result.DocumentsExcluded}");
        Console.WriteLine($"Documents skipped:    {result.DocumentsSkipped}");
        Console.WriteLine($"Documents failed:     {result.DocumentsFailed}");
        Console.WriteLine($"Completed:            {result.Completed}");
        Console.WriteLine($"Continuation token:   {(String.IsNullOrWhiteSpace(result.ContinuationToken) ? "<none>" : "present")}");
        Console.WriteLine();
        Console.WriteLine($"{"Collection",-20} {"EntityType",-40} {"Read",8} {"Excluded",10} {"Written",9} {"Failed",8}");

        foreach (var route in result.Routes
                     .OrderBy(item => item.CollectionName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.EntityType, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{route.CollectionName,-20} {DisplayEntityType(route.EntityType),-40} {route.Read,8} {route.Excluded,10} {route.Written,9} {route.Failed,8}");
        }
    }

    private static string DisplayEntityType(string entityType) => String.IsNullOrWhiteSpace(entityType) ? "<missing>" : entityType;
}
