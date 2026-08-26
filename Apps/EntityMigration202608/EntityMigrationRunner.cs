using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Utils;
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
        var request = CreateRequest(true, batchSize, maxPages, continuationToken);
        PrintPlan("DRY RUN", request);
        var result = await _migrationService.MigrateCosmosToMongoAsync(request, ct).ConfigureAwait(false);
        PrintMigrationResult(result);
    }

    public async Task MigrateAsync(int batchSize = 200, int maxPages = 0, string continuationToken = null, CancellationToken ct = default)
    {
        var request = CreateRequest(false, batchSize, maxPages, continuationToken);
        PrintPlan("WRITE", request);

        Console.WriteLine();
        Console.Write("Type MIGRATE to copy eligible entity documents to Mongo: ");
        if (!String.Equals(Console.ReadLine(), "MIGRATE", StringComparison.Ordinal))
        {
            Console.WriteLine("Migration cancelled.");
            return;
        }

        var result = await _migrationService.MigrateCosmosToMongoAsync(request, ct).ConfigureAwait(false);
        PrintMigrationResult(result);
    }

    public async Task ValidateAsync(int batchSize = 200, CancellationToken ct = default)
    {
        var request = CreateRequest(true, batchSize, 0, null);
        PrintPlan("VALIDATE", request);
        var result = await _migrationService.ValidateCosmosToMongoAsync(request, ct).ConfigureAwait(false);

        Console.WriteLine();
        Console.WriteLine($"Source eligible:      {result.SourceCount}");
        Console.WriteLine($"Mongo destination:    {result.DestinationCount}");
        Console.WriteLine($"Matches:              {result.Matches}");
        Console.WriteLine();
        Console.WriteLine($"{"Collection",-20} {"EntityType",-40} {"Source",10} {"Mongo",10} {"Match",7}");

        foreach (var route in result.Routes
                     .OrderBy(item => item.CollectionName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.EntityType, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{route.CollectionName,-20} {DisplayEntityType(route.EntityType),-40} {route.SourceCount,10} {route.DestinationCount,10} {(route.Matches ? "yes" : "NO"),7}");
        }
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

    private CosmosToMongoMigrationRequest CreateRequest(bool dryRun, int batchSize, int maxPages, string continuationToken)
    {
        return new CosmosToMongoMigrationRequest
        {
            Source = _source,
            Target = _target,
            BatchSize = batchSize,
            MaxPages = maxPages,
            ContinuationToken = continuationToken,
            DryRun = dryRun,
            ExcludedEntityTypes = ExcludedEntityTypes.ToArray()
        };
    }

    private void PrintPlan(string operation, CosmosToMongoMigrationRequest request)
    {
        Console.WriteLine($"August 2026 entity migration - {operation}");
        Console.WriteLine($"Environment:          {_environment}");
        Console.WriteLine($"Cosmos database:      {_source.DatabaseName}");
        Console.WriteLine($"Cosmos collection:    {_source.DatabaseName}_Collections");
        Console.WriteLine($"Mongo database:       {_target.DatabaseName}");
        Console.WriteLine($"Mongo collections:    {String.Join(", ", DestinationCollections)}");
        Console.WriteLine($"Batch size:           {request.BatchSize}");
        Console.WriteLine($"Max pages:            {(request.MaxPages <= 0 ? "all" : request.MaxPages)}");
        Console.WriteLine($"Continuation token:   {(String.IsNullOrWhiteSpace(request.ContinuationToken) ? "<none>" : "present")}");
        Console.WriteLine($"Excluded EntityTypes: {(ExcludedEntityTypes.Count == 0 ? "<none>" : String.Join(", ", ExcludedEntityTypes.OrderBy(item => item)))}");
    }

    private void PrintTarget()
    {
        Console.WriteLine("August 2026 entity migration reset");
        Console.WriteLine($"Environment:          {_environment}");
        Console.WriteLine($"Mongo database:       {_target.DatabaseName}");
        Console.WriteLine($"Mongo collections:    {String.Join(", ", DestinationCollections)}");
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
