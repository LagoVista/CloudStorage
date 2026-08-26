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
            await MigrateAsync(catalog, catalog.LoadByKey(args[1]), HasOption(args, "--catch-up"));
            break;
        case "verify":
            RequireKey(args, command);
            await VerifyAsync(catalog, catalog.LoadByKey(args[1]));
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

static async Task MigrateAsync(MigrationCatalog catalog, MigrationDefinition definition, bool catchUp)
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
    Console.WriteLine();

    PrintState(await engine.ExecuteAsync(definition, sha, catchUp));
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

static void RequireKey(string[] args, string command) { if (args.Length < 2) throw new ArgumentException($"{command} requires a migration key."); }
static bool HasOption(string[] args, string option) => args.Any(arg => String.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
static void PrintUsage()
{
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  catalog");
    Console.Error.WriteLine("  validate <migration-key>");
    Console.Error.WriteLine("  status <migration-key>");
    Console.Error.WriteLine("  probe <migration-key>");
    Console.Error.WriteLine("  migrate <migration-key> [--catch-up]");
    Console.Error.WriteLine("  verify <migration-key>");
    Console.Error.WriteLine("Environment: set MIGRATION_ENVIRONMENT=dev|prod for environment-prefixed storage settings.");
}
