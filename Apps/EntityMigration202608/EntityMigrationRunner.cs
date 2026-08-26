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
    }

    public async Task DryRunAsync(int batchSize = 200, int maxPages = 0, string continuationToken = null, CancellationToken ct = default)
    {
        PrintPlan("DRY RUN", batchSize, maxPages, continuationToken);

        foreach (var sourceCollectionName in GetSourceCollections())
        {
            Console.WriteLine();
            Console.WriteLine($"Source collection: {sourceCollectionName}");
            var request = CreateRequest(sourceCollectionName, true, batchSize, maxPages, continuationToken);
            var result = await _migrationService.MigrateCosmosToMongoAsync(request, ct).ConfigureAwait(false);
            PrintMigrationResult(result);
        }
    }

    public async Task MigrateAsync(int batchSize = 200, int maxPages = 0, string continuationToken = null, CancellationToken ct = default)
    {
        PrintPlan("WRITE", batchSize, maxPages, continuationToken);

        Console.WriteLine();
        Console.Write("Type MIGRATE to copy eligible entity documents to Mongo: ");
        if (!String.Equals(Console.ReadLine(), "MIGRATE", StringComparison.Ordinal))
        {
            Console.WriteLine("Migration cancelled.");
            return;
        }

        foreach (var sourceCollectionName in GetSourceCollections())
        {
            Console.WriteLine();
            Console.WriteLine($"Source collection: {sourceCollectionName}");
            var request = CreateRequest(sourceCollectionName, false, batchSize, maxPages, continuationToken);
            var result = await _migrationService.MigrateCosmosToMongoAsync(request, ct).ConfigureAwait(false);
            PrintMigrationResult(result);
        }
    }

    public async Task ValidateAsync(int batchSize = 200, CancellationToken ct = default)
    {
        PrintPlan("VALIDATE", batchSize, 0, null);

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
        Console.WriteLine($"This will permanently drop: {String.Join(", ", DestinationCollections)}.");
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

    private void PrintPlan(string operation, int batchSize, int maxPages, string continuationToken)
    {
        Console.WriteLine($"August 2026 entity migration - {operation}");
        Console.WriteLine($"Environment:          {_environment}");
        Console.WriteLine($"Cosmos database:      {_source.DatabaseName}");
        Console.WriteLine($"Cosmos collections:   {String.Join(", ", GetSourceCollections())}");
        Console.WriteLine($"Mongo database:       {_target.DatabaseName}");
        Console.WriteLine($"Mongo collections:    {String.Join(", ", DestinationCollections)}");
        Console.WriteLine($"Batch size:           {batchSize}");
        Console.WriteLine($"Max pages/source:     {(maxPages <= 0 ? "all" : maxPages)}");
        Console.WriteLine($"Continuation token:   {(String.IsNullOrWhiteSpace(continuationToken) ? "<none>" : "present")}");
        Console.WriteLine($"Excluded EntityTypes: {(ExcludedEntityTypes.Count == 0 ? "<none>" : String.Join(", ", ExcludedEntityTypes.OrderBy(item => item)))}");

        if (!String.IsNullOrWhiteSpace(continuationToken))
            Console.WriteLine("NOTE: the supplied continuation token is applied to each Cosmos source collection independently.");
    }

    private void PrintTarget()
    {
        Console.WriteLine("August 2026 entity migration reset");
        Console.WriteLine($"Environment:          {_environment}");
        Console.WriteLine($"Mongo database:       {_target.DatabaseName}");
        Console.WriteLine($"Mongo collections:    {String.Join(", ", DestinationCollections)}");
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
        Console.WriteLine($"Continuation token:   {(String.IsNullOrWhiteSpace(result.ContinuationToken) ? "<none>" : result.ContinuationToken)}");
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
