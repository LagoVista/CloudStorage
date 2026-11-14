using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LagoVista.Relational.DataContexts;
using LagoVista.Relational.MigrationRuntime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

// Export the static Config Server registration contract. Like other short-lived
// LagoVista workers, this command must work without bootstrap credentials,
// database access or external services.
if (args.Contains("--export-config-manifest", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(MigrationConfigurationContract.ExportConfigManifest());
    return;
}

// Source-only engineering analysis, invoked by Build Server on the exact
// committed checkout. No Config Server lookup and no database connection.
if (args.Contains("--analyze-model", StringComparer.OrdinalIgnoreCase))
{
    var analysisOptions = new DbContextOptionsBuilder<SemanticPostgresMigrationContext>()
        .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
        .Options;
    await using var analysis = new SemanticPostgresMigrationContext(analysisOptions);
    var knownMigrations = analysis.Database.GetMigrations().ToArray();
    if (knownMigrations.Length == 0)
        throw new InvalidOperationException("No semantic PostgreSQL EF migrations exist in this image.");
    var changed = analysis.Database.HasPendingModelChanges();
    Console.WriteLine($"MODEL_ANALYSIS job=semantic-schema-migrator context=SemanticPostgresMigrationContext provider=postgresql target={knownMigrations[^1]} drift={(changed ? "required" : "none")}");
    Environment.ExitCode = changed ? 2 : 0;
    return;
}

// A target is authorized by the platform even before its physical database exists.
// This runner owns create-if-missing + forward-only EF convergence for one schema family.
var target = (Environment.GetEnvironmentVariable("MIGRATION_TARGET") ?? "").Trim().ToLowerInvariant();
var expectedDatabase = target switch
{
    "production" => "semantic_production",
    "test" => "semantic_test",
    _ => throw new InvalidOperationException("MIGRATION_TARGET must be 'production' or 'test'.")
};

var action = (Environment.GetEnvironmentVariable("MIGRATION_ACTION") ?? "status").Trim().ToLowerInvariant();
if (action is not ("status" or "apply"))
    throw new InvalidOperationException("MIGRATION_ACTION must be 'status' or 'apply'.");

// Shared .NET runtime uses Config Server when a remote bootstrap is provided.
// Direct secret mode remains available for the existing DEV-only pilot scripts.
var resolvedConfiguration = await MigrationConfigurationLoader.LoadAsync(
    MigrationConfigurationContract.ApplicationKey, target, expectedDatabase, "postgresql");
var dbConnection = new NpgsqlConnectionStringBuilder(resolvedConfiguration.ConnectionString);
if (!string.Equals(dbConnection.Database, expectedDatabase, StringComparison.Ordinal))
    throw new InvalidOperationException("The supplied migration connection does not match the authorized semantic catalog target.");
if (string.IsNullOrWhiteSpace(dbConnection.Host) || string.IsNullOrWhiteSpace(dbConnection.Username))
    throw new InvalidOperationException("Migration connection requires PostgreSQL host and username.");
if (resolvedConfiguration.Source == "config-server" &&
    !string.Equals(dbConnection.Host, resolvedConfiguration.ServerHost, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Config Server connection host does not match the registered migration target server.");

Console.WriteLine($"Migration configuration: {resolvedConfiguration.Source}; deployment: {resolvedConfiguration.Deployment}; target: {target}; server host: {dbConnection.Host}; database: {expectedDatabase}");

// Hold the same PostgreSQL advisory lock for the *entire* apply, not just
// CREATE DATABASE. This serializes schema initialization and EF migrations
// across separately launched/retied Jobs, including when the DB is absent.
await using var applyLease = action == "apply"
    ? await SemanticMigrationApplyLease.AcquireAsync(
        dbConnection, expectedDatabase)
    : null;

var options = new DbContextOptionsBuilder<SemanticPostgresMigrationContext>()
    .UseNpgsql(dbConnection.ConnectionString, provider => provider.EnableRetryOnFailure())
    .Options;
await using var context = new SemanticPostgresMigrationContext(options);
var databaseCreator = context.GetService<IRelationalDatabaseCreator>();
var exists = await databaseCreator.ExistsAsync();

Console.WriteLine($"Target: semantic-{target}; action: {action}; database state: {(exists ? "exists" : "missing")}");
if (!exists && action == "status")
{
    // Read-only readiness: the configured migrator identity must have
    // permission to create and own the target database.
    // No credentials or SQL mutation is included in the status result.
    var creationReady = await SemanticMigrationApplyLease.CheckCreatePermissionAsync(
        dbConnection, expectedDatabase);
    Console.WriteLine(creationReady ? "CREATE_PRIVILEGE: READY" : "CREATE_PRIVILEGE: BLOCKED");
    Console.WriteLine("MISSING: target database is authorized but not yet present; applying this image will create it.");
    return;
}

if (!exists)
{
    // The same PostgreSQL identity creates, owns, and migrates the database.
    // No second bootstrap credential is needed.
    await CreateDatabaseIfMissingAsync(dbConnection, expectedDatabase);
    exists = await databaseCreator.ExistsAsync();
    if (!exists)
        throw new InvalidOperationException("Database creation returned without the target database becoming available.");
    Console.WriteLine("Created the approved target database.");
}

var known = context.Database.GetMigrations().ToArray();
if (known.Length == 0)
    throw new InvalidOperationException("The image does not contain the required PostgreSQL semantic migrations.");
var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
var knownSet = new HashSet<string>(known, StringComparer.Ordinal);
var unknown = applied.Where(x => !knownSet.Contains(x)).ToArray();
if (unknown.Length > 0)
    throw new InvalidOperationException($"Target contains unknown or newer EF migration IDs: {string.Join(", ", unknown)}.");
if (!applied.SequenceEqual(known.Take(applied.Length), StringComparer.Ordinal))
    throw new InvalidOperationException("Target migration history is not a supported prefix of this image's migrations.");

// An existing database with semantic tables but no recorded migrations must
// never be adopted or reinitialized implicitly.
if (applied.Length == 0 && await ContainsExistingSemanticTablesAsync(dbConnection.ConnectionString))
    throw new InvalidOperationException("Existing semantic schema has no recognized EF migration history; refusing to overwrite or adopt it.");

var pending = known.Skip(applied.Length).ToArray();
Console.WriteLine($"Current revision: {(applied.Length == 0 ? "uninitialized" : applied[^1])}; target revision: {known[^1]}");
foreach (var migration in pending)
    Console.WriteLine($"PENDING {migration}");

if (action == "status")
{
    // A matching EF history row alone is not proof that the schema is usable.
    // When CURRENT, independently probe every mapped semantic table and the view.
    // Incomplete targets stay BEHIND without attempting queries against objects
    // that the pending migrations have not created yet.
    if (pending.Length == 0)
        await VerifySemanticSchemaAsync(context);
    Console.WriteLine(pending.Length == 0 ? "CURRENT: no migrations required; semantic schema verified." : $"BEHIND: {pending.Length} migration(s) required.");
    return;
}

if (pending.Length > 0)
{
    await context.Database.MigrateAsync();
    Console.WriteLine($"Applied {pending.Length} pending migration(s).");
}
else
{
    Console.WriteLine("No-op: database already at target revision.");
}

var finalApplied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
if (!finalApplied.SequenceEqual(known, StringComparer.Ordinal))
    throw new InvalidOperationException("Database migration history did not converge to the image's target revision.");

await VerifySemanticSchemaAsync(context);
Console.WriteLine($"VERIFIED: schema revision {known[^1]} and seven semantic tables plus view.");

static async Task VerifySemanticSchemaAsync(SemanticPostgresMigrationContext context)
{
    // Read-only verification; a SELECT against an empty table still catches
    // missing relations, incorrect schemas, and mismatched mapped columns.
    // Run on every completed apply AND on every CURRENT status inspection.
    await context.Registry.AsNoTracking().Take(1).ToListAsync();
    await context.Subjects.AsNoTracking().Take(1).ToListAsync();
    await context.Concepts.AsNoTracking().Take(1).ToListAsync();
    await context.Definitions.AsNoTracking().Take(1).ToListAsync();
    await context.DefinitionView.AsNoTracking().Take(1).ToListAsync();
    await context.Embeddings.AsNoTracking().Take(1).ToListAsync();
    await context.Artifacts.AsNoTracking().Take(1).ToListAsync();
    await context.ArtifactInformationElements.AsNoTracking().Take(1).ToListAsync();
    Console.WriteLine("SCHEMA_VERIFICATION: PASS; seven semantic tables and definition view queryable.");
}

static async Task<bool> ContainsExistingSemanticTablesAsync(string connectionString)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var cmd = new NpgsqlCommand(
        "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='dbo' AND table_name = ANY (ARRAY['registry','subjects','concepts','definitions','embeddings','artifacts','artifactinformationelements']))",
        connection);
    return (bool)(await cmd.ExecuteScalarAsync() ?? false);
}

static async Task CreateDatabaseIfMissingAsync(NpgsqlConnectionStringBuilder target, string databaseName)
{
    // Connect to the existing postgres maintenance database as the SAME
    // migration user, on the same server with the same credentials.
    var maintenance = SemanticMigrationApplyLease.MaintenanceConnection(target, databaseName);
    await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
    await connection.OpenAsync();

    // A caller-owned advisory lock covers creation and subsequent EF migration.
    await using var existsCmd = new NpgsqlCommand(
        "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname=@database)", connection);
    existsCmd.Parameters.AddWithValue("database", databaseName);
    if ((bool)(await existsCmd.ExecuteScalarAsync() ?? false))
        return;

    // Names come from the finite, image-owned allowlist.
    var quotedDatabase = "\"" + databaseName.Replace("\"", "\"\"") + "\"";
    var quotedOwner = "\"" + target.Username.Replace("\"", "\"\"") + "\"";
    await using var createCmd = new NpgsqlCommand(
        $"CREATE DATABASE {quotedDatabase} OWNER {quotedOwner}", connection);
    try
    {
        await createCmd.ExecuteNonQueryAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42501")
    {
        throw new InvalidOperationException(
            "The configured db-migrator PostgreSQL account cannot CREATE DATABASE. " +
            "Grant CREATEDB to the selected account, then rerun DEV status.", ex);
    }
}

internal sealed class SemanticMigrationApplyLease : IAsyncDisposable
{
    private const string LockNamespace = "nuviot:semantic-schema-migrator:";
    private readonly NpgsqlConnection _connection;
    private readonly string _lockName;

    private SemanticMigrationApplyLease(NpgsqlConnection connection, string lockName)
    {
        _connection = connection;
        _lockName = lockName;
    }

    public static async Task<bool> CheckCreatePermissionAsync(
        NpgsqlConnectionStringBuilder target, string database)
    {
        var maintenance = MaintenanceConnection(target, database);
        try
        {
            await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
            await connection.OpenAsync();
            // Database creator and owner are the same identity, so only
            // CREATEDB (or SUPERUSER) is required, not SET ROLE membership.
            await using var command = new NpgsqlCommand(
                "SELECT rolcreatedb OR rolsuper FROM pg_roles WHERE rolname = current_user",
                connection);
            return (bool)(await command.ExecuteScalarAsync() ?? false);
        }
        catch (PostgresException)
        {
            // Missing/denied maintenance database access blocks creation.
            return false;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    internal static NpgsqlConnectionStringBuilder MaintenanceConnection(
        NpgsqlConnectionStringBuilder target, string database)
    {
        if (database is not ("semantic_test" or "semantic_production") ||
            !string.Equals(target.Database, database, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(target.Host) ||
            string.IsNullOrWhiteSpace(target.Username))
            throw new InvalidOperationException("Only approved semantic database identities may be managed.");

        // Only switch database names; preserve host, port, username,
        // password and SSL policy from the one registered migration account.
        return new NpgsqlConnectionStringBuilder(target.ConnectionString) { Database = "postgres" };
    }

    public static async Task<SemanticMigrationApplyLease> AcquireAsync(
        NpgsqlConnectionStringBuilder target, string database)
    {
        var maintenance = MaintenanceConnection(target, database);

        var connection = new NpgsqlConnection(maintenance.ConnectionString);
        try
        {
            await connection.OpenAsync();
            var lockName = LockNamespace + database;
            await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(hashtext(@name))", connection);
            command.Parameters.AddWithValue("name", lockName);
            await command.ExecuteNonQueryAsync();
            return new SemanticMigrationApplyLease(connection, lockName);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtext(@name))", _connection);
            command.Parameters.AddWithValue("name", _lockName);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            await _connection.DisposeAsync();
        }
    }
}
