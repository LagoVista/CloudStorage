using EntityMigration202608;

var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "status";
var environment = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "dev";
var maxPages = args.Length > 2 && Int32.TryParse(args[2], out var parsedMaxPages) ? parsedMaxPages : 5;
var batchSize = args.Length > 3 && Int32.TryParse(args[3], out var parsedBatchSize) ? parsedBatchSize : 200;

var shutdownCts = new CancellationTokenSource();

void RequestShutdown()
{
    try
    {
        if (!shutdownCts.IsCancellationRequested)
            shutdownCts.Cancel();
    }
    catch (ObjectDisposedException)
    {
        // Process shutdown may race with normal cleanup.
    }
}

ConsoleCancelEventHandler cancelHandler = (_, e) =>
{
    Console.WriteLine("Ctrl+C received, shutting down...");
    e.Cancel = true;
    RequestShutdown();
};

EventHandler processExitHandler = (_, _) => RequestShutdown();

Console.CancelKeyPress += cancelHandler;
AppDomain.CurrentDomain.ProcessExit += processExitHandler;

try
{
    var runner = new EntityMigrationRunner(environment);

    switch (mode)
    {
        case "status":
            await runner.ShowStatusAsync(shutdownCts.Token);
            break;

        case "dry-run":
        case "migrate-entities-dryrun":
            await runner.DryRunAsync(batchSize, maxPages, shutdownCts.Token);
            break;

        case "migrate":
        case "migrate-entities":
            await runner.MigrateAsync(batchSize, maxPages, shutdownCts.Token);
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
            Console.WriteLine("Usage: EntityMigration202608 <status|dry-run|migrate|validate|reset> [prod|dev] [maxPages] [batchSize]");
            Environment.ExitCode = 1;
            break;
    }
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
    AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
    shutdownCts.Dispose();
}
