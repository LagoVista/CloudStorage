using LagoVista.StorageMigration;

var definitionDirectory = Path.Combine(AppContext.BaseDirectory, "Definitions");
var catalog = new MigrationCatalog(definitionDirectory);
var command = args.Length == 0 ? "catalog" : args[0].ToLowerInvariant();

try
{
    switch (command)
    {
        case "catalog":
            foreach (var item in catalog.LoadAll()) Console.WriteLine($"{item.Key,-36} {item.Target.Table,-24} {item.Source.TableName}{item.Source.TablePattern}");
            break;
        case "validate":
            RequireKey(args, command);
            PrintDefinition(catalog, catalog.LoadByKey(args[1]));
            break;
        case "status":
            RequireKey(args, command);
            await PrintStatusAsync(catalog.LoadByKey(args[1]));
            break;
        case "probe":
            RequireKey(args, command);
            await ProbeAsync(catalog.LoadByKey(args[1]));
            break;
        case "migrate":
            RequireKey(args, command);
            await MigrateAsync(catalog, catalog.LoadByKey(args[1]), HasOption(args, "--catch-up"), GetPositiveIntOption(args, "--max-records"));
            break;
        case "verify":
            RequireKey(args, command);
            await VerifyAsync(catalog, catalog.LoadByKey(args[1]));
            break;
        case "object-probe":
            await ObjectProbeAsync(GetPositiveIntOption(args, "--max-objects"));
            break;
        case "object-status":
            await ObjectStatusAsync();
            break;
        case "object-migrate":
            await ObjectMigrateAsync(GetPositiveIntOption(args, "--max-objects"));
            break;
        default:
            PrintUsage();
            Environment.ExitCode = 2;
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAIL: {ex}");
    Environment.ExitCode = 1;
}

static ApplicationDataMigrationStateStore StateStore() => ApplicationDataMigrationStateStore.Create(MigrationConnections.EnvironmentName);

static async Task ObjectProbeAsync(int? maxObjects)
{
    Console.WriteLine("Object storage migration probe");
    Console.WriteLine($"Environment: {MigrationConnections.EnvironmentName}");
    Console.WriteLine($"Scan limit : {(maxObjects.HasValue ? $"{maxObjects.Value:N0} objects" : "none")}");
    Console.WriteLine();

    Console.WriteLine("[1/2] Connecting to SeaweedFS S3 endpoint...");
    var s3Probe = new S3ObjectStorageProbe(MigrationConnections.S3ObjectStorage);
    var buckets = await s3Probe.ListBucketsAsync();
    Console.WriteLine($"PASS: S3 endpoint is reachable; {buckets.Count:N0} bucket(s) currently visible.");
    foreach (var bucket in buckets) Console.WriteLine($"  {bucket}");
    Console.WriteLine();

    Console.WriteLine("[2/2] Inventorying Azure Blob Storage metadata...");
    var source = new AzureBlobObjectInventorySource(MigrationConnections.AzureBlobConnectionString());
    var inventory = await source.InventoryAsync(maxObjects);

    Console.WriteLine();
    Console.WriteLine($"{"Container",-36} {"Objects",12} {"Bytes",16} {"Size",12} {"Oldest",22} {"Newest",22}");
    Console.WriteLine(new String('-', 128));
    foreach (var container in inventory.Containers.OrderByDescending(item => item.TotalBytes))
    {
        Console.WriteLine(
            $"{container.Container,-36} " +
            $"{container.ObjectCount,12:N0} " +
            $"{container.TotalBytes,16:N0} " +
            $"{FormatBytes(container.TotalBytes),12} " +
            $"{FormatDate(container.OldestLastModified),22} " +
            $"{FormatDate(container.NewestLastModified),22}");
    }

    Console.WriteLine(new String('-', 128));
    Console.WriteLine($"TOTAL                                {inventory.ObjectCount,12:N0} {inventory.TotalBytes,16:N0} {FormatBytes(inventory.TotalBytes),12}");
    if (inventory.WasLimited)
        Console.WriteLine("NOTE: inventory was intentionally limited; totals are partial.");
    Console.WriteLine();
    Console.WriteLine("PASS: object storage probe completed. No blobs were copied or modified.");
}

static async Task ObjectMigrateAsync(int? maxObjects)
{
    Console.WriteLine("Azure Blob -> S3 object migration");
    Console.WriteLine($"Environment: {MigrationConnections.EnvironmentName}");
    Console.WriteLine($"Run limit : {(maxObjects.HasValue ? $"{maxObjects.Value:N0} objects" : "none")}");
    Console.WriteLine("Mode      : resume from Application Data checkpoint");
    Console.WriteLine();

    var engine = new AzureBlobToS3Migration(
        MigrationConnections.AzureBlobConnectionString(),
        MigrationConnections.S3ObjectStorage,
        StateStore());

    var state = await engine.ExecuteAsync(maxObjects);
    PrintObjectState(state);
}

static async Task ObjectStatusAsync()
{
    var state = await StateStore().GetAsync(AzureBlobToS3Migration.MigrationKey);
    if (state == null)
    {
        Console.WriteLine($"{AzureBlobToS3Migration.MigrationKey}: Not Started");
        return;
    }

    PrintObjectState(state);
}

static async Task ProbeAsync(MigrationDefinition definition)
{
    Console.WriteLine($"Probing migration: {definition.DisplayName}");
    Console.WriteLine($"Environment      : {MigrationConnections.EnvironmentName}");
    Console.WriteLine($"Source connection: {definition.Source.Connection}");
    Console.WriteLine($"Target table     : {definition.Target.Table}");
    Console.WriteLine();

    Console.WriteLine("[1/2] Resolving Azure Table source...");
    var source = new AzureTableMigrationSource(MigrationConnections.AzureTableConnectionString(definition.Source.Connection));
    var tables = await source.ResolveTablesAsync(definition);
    if (tables.Count == 0)
        throw new InvalidOperationException($"No Azure source tables matched migration definition '{definition.Key}'.");

    foreach (var table in tables)
        Console.WriteLine($"  {table}");
    Console.WriteLine($"PASS: resolved {tables.Count:N0} source table(s).");
    Console.WriteLine();

    Console.WriteLine("[2/2] Connecting to Cassandra and validating target schema...");
    using var writer = new CassandraActivityRecordMigrationWriter(MigrationConnections.Cassandra);
    await writer.EnsureSchemaAsync(definition);
    Console.WriteLine($"PASS: Cassandra target '{definition.Target.Table}' is ready.");
    Console.WriteLine();
    Console.WriteLine("PASS: migration probe completed. No records were migrated.");
}

static async Task MigrateAsync(MigrationCatalog catalog, MigrationDefinition definition, bool catchUp, int? maxRecords)
{
    var sha = catalog.DefinitionSha256(definition);
    var source = new AzureTableMigrationSource(MigrationConnections.AzureTableConnectionString(definition.Source.Connection));
    using var writer = new CassandraActivityRecordMigrationWriter(MigrationConnections.Cassandra);
    var engine = new StorageMigrationEngine(source, new AzureTableRecordMapper(), writer, StateStore());

    Console.WriteLine($"Migration : {definition.DisplayName}");
    Console.WriteLine($"Definition: {sha}");
    Console.WriteLine($"Source    : {definition.Source.Connection}");
    Console.WriteLine($"Target    : {definition.Target.Table}");
    Console.WriteLine($"Mode      : {(catchUp ? "catch-up replay" : "resume/current pass")}");
    Console.WriteLine($"Run limit : {(maxRecords.HasValue ? $"{maxRecords.Value:N0} records" : "none")}");
    Console.WriteLine();

    PrintState(await engine.ExecuteAsync(definition, sha, catchUp, maxRecords));
}

static async Task VerifyAsync(MigrationCatalog catalog, MigrationDefinition definition)
{
    var sha = catalog.DefinitionSha256(definition);
    var source = new AzureTableMigrationSource(MigrationConnections.AzureTableConnectionString(definition.Source.Connection));
    var state = await StateStore().GetAsync(definition.Key);
    if (state == null) throw new InvalidOperationException($"No migration state exists for {definition.Key}.");
    if (!String.Equals(state.DefinitionSha256, sha, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The completed migration state does not match the current definition SHA.");

    Console.WriteLine($"Verifying : {definition.DisplayName}");
    Console.WriteLine($"Definition: {sha}");
    Console.WriteLine("Counting Azure source records...");
    var sourceCount = await source.CountAsync(definition);
    var completed = String.Equals(state.State, "Completed", StringComparison.OrdinalIgnoreCase);
    var countsMatch = sourceCount == state.RecordsWritten;
    var noFailures = state.RecordsFailed == 0;
    Console.WriteLine($"Source records : {sourceCount:N0}");
    Console.WriteLine($"Records written: {state.RecordsWritten:N0}");
    Console.WriteLine($"Failures       : {state.RecordsFailed:N0}");
    Console.WriteLine($"State          : {state.State}");
    Console.WriteLine($"{(completed ? "PASS" : "FAIL")}: migration completed");
    Console.WriteLine($"{(countsMatch ? "PASS" : "FAIL")}: source count matches records written");
    Console.WriteLine($"{(noFailures ? "PASS" : "FAIL")}: no migration failures");
    if (!completed || !countsMatch || !noFailures) throw new InvalidOperationException("Migration verification failed.");
}

static async Task PrintStatusAsync(MigrationDefinition definition)
{
    var state = await StateStore().GetAsync(definition.Key);
    if (state == null) { Console.WriteLine($"{definition.Key}: Not Started"); return; }
    PrintState(state);
}

static void PrintObjectState(MigrationRunState state)
{
    Console.WriteLine($"State          : {state.State}");
    Console.WriteLine($"Container      : {state.CurrentTable ?? "<none>"}");
    Console.WriteLine($"Object         : {state.HeadRowKey ?? "<none>"}");
    Console.WriteLine($"Objects read   : {state.RecordsRead:N0}");
    Console.WriteLine($"Objects written: {state.RecordsWritten:N0}");
    Console.WriteLine($"Objects failed : {state.RecordsFailed:N0}");
    Console.WriteLine($"Bytes read     : {state.BytesRead:N0} ({FormatBytes(state.BytesRead)})");
    Console.WriteLine($"Bytes written  : {state.BytesWritten:N0} ({FormatBytes(state.BytesWritten)})");
    if (state.CompletedDate.HasValue) Console.WriteLine($"Completed      : {state.CompletedDate.Value:u}");
}

static void PrintState(MigrationRunState state)
{
    Console.WriteLine($"State          : {state.State}");
    Console.WriteLine($"Pass           : {Math.Max(1, state.PassNumber)}");
    Console.WriteLine($"Current table  : {state.CurrentTable ?? "<none>"}");
    Console.WriteLine($"Head partition : {state.HeadPartitionKey ?? "<none>"}");
    Console.WriteLine($"Head row       : {state.HeadRowKey ?? "<none>"}");
    Console.WriteLine($"Records read   : {state.RecordsRead:N0}");
    Console.WriteLine($"Records written: {state.RecordsWritten:N0}");
    Console.WriteLine($"Records failed : {state.RecordsFailed:N0}");
    if (state.PriorPassRecordsWritten > 0 || state.PriorPassRecordsRead > 0 || state.PriorPassRecordsFailed > 0)
    {
        Console.WriteLine($"Prior read     : {state.PriorPassRecordsRead:N0}");
        Console.WriteLine($"Prior written  : {state.PriorPassRecordsWritten:N0}");
        Console.WriteLine($"Prior failed   : {state.PriorPassRecordsFailed:N0}");
    }
    if (state.CompletedDate.HasValue) Console.WriteLine($"Completed      : {state.CompletedDate.Value:u}");
}

static void PrintDefinition(MigrationCatalog catalog, MigrationDefinition definition)
{
    Console.WriteLine($"PASS: {definition.Key}");
    Console.WriteLine($"Definition SHA-256: {catalog.DefinitionSha256(definition)}");
    Console.WriteLine($"Source connection: {definition.Source.Connection}");
    Console.WriteLine($"Target table: {definition.Target.Table}");
    Console.WriteLine($"Partition: {String.Join(", ", definition.Target.PartitionFields)} + {definition.Target.Bucket}");
    Console.WriteLine($"Indexes: {String.Join(", ", definition.Target.Indexes)}");
    Console.WriteLine($"Retention seconds: {(definition.Target.RetentionSeconds?.ToString() ?? "forever")}");
}

static string FormatDate(DateTimeOffset? value) => value.HasValue ? value.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") : "<none>";

static string FormatBytes(long bytes)
{
    string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
    double value = bytes;
    var suffix = 0;
    while (value >= 1024 && suffix < suffixes.Length - 1)
    {
        value /= 1024;
        suffix++;
    }
    return $"{value:0.##} {suffixes[suffix]}";
}

static void RequireKey(string[] args, string command) { if (args.Length < 2) throw new ArgumentException($"{command} requires a migration key."); }
static bool HasOption(string[] args, string option) => args.Any(arg => String.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
static int? GetPositiveIntOption(string[] args, string option)
{
    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        string? value = null;
        if (String.Equals(arg, option, StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length) throw new ArgumentException($"{option} requires a positive integer value.");
            value = args[index + 1];
        }
        else if (arg.StartsWith(option + "=", StringComparison.OrdinalIgnoreCase))
        {
            value = arg.Substring(option.Length + 1);
        }

        if (value != null)
        {
            if (!Int32.TryParse(value, out var parsed) || parsed <= 0)
                throw new ArgumentException($"{option} requires a positive integer value.");
            return parsed;
        }
    }

    return null;
}
static void PrintUsage()
{
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  catalog");
    Console.Error.WriteLine("  validate <migration-key>");
    Console.Error.WriteLine("  status <migration-key>");
    Console.Error.WriteLine("  probe <migration-key>");
    Console.Error.WriteLine("  migrate <migration-key> [--max-records N] [--catch-up]");
    Console.Error.WriteLine("  verify <migration-key>");
    Console.Error.WriteLine("  object-probe [--max-objects N]");
    Console.Error.WriteLine("  object-status");
    Console.Error.WriteLine("  object-migrate [--max-objects N]");
    Console.Error.WriteLine("Environment: set MIGRATION_ENVIRONMENT=dev|prod for environment-prefixed storage settings.");
}
