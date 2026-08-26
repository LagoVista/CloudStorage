using EntityMigration202608;

var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "dry-run";
var environment = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "prod";
var maxPages = args.Length > 2 && Int32.TryParse(args[2], out var parsedMaxPages) ? parsedMaxPages : 5;
var batchSize = args.Length > 3 && Int32.TryParse(args[3], out var parsedBatchSize) ? parsedBatchSize : 200;
var continuationToken = args.Length > 4 ? args[4] : null;

using var shutdownCts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("Ctrl+C received, shutting down...");
    e.Cancel = true;
    shutdownCts.Cancel();
};

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!shutdownCts.IsCancellationRequested)
        shutdownCts.Cancel();
};

var runner = new EntityMigrationRunner(environment);

switch (mode)
{
    case "dry-run":
    case "migrate-entities-dryrun":
        await runner.DryRunAsync(batchSize, maxPages, continuationToken, shutdownCts.Token);
        break;

    case "migrate":
    case "migrate-entities":
        await runner.MigrateAsync(batchSize, maxPages, continuationToken, shutdownCts.Token);
        break;

    case "validate":
    case "validate-entities":
        await runner.ValidateAsync(batchSize, shutdownCts.Token);
        break;

    case "reset":
    case "reset-mongo-entities":
        await runner.ResetMongoAsync(shutdownCts.Token);
        break;

    default:
        Console.WriteLine($"Unknown mode '{mode}'.");
        Console.WriteLine("Usage: EntityMigration202608 <dry-run|migrate|validate|reset> [prod|dev] [maxPages] [batchSize] [continuationToken]");
        Environment.ExitCode = 1;
        break;
}
