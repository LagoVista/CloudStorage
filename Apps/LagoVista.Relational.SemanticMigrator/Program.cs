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
    "semantic-schema-migrator", target, expectedDatabase, "postgresql");
var dbConnection = new NpgsqlConnectionStringBuilder(resolvedConfiguration.ConnectionString);
if (!string.Equals(dbConnection.Database, expectedDatabase, StringComparison.Ordinal))
    throw new InvalidOperationException("The supplied migration connection does not match the authorized semantic catalog target.");
if (string.IsNullOrWhiteSpace(dbConnection.Host) || string.IsNullOrWhiteSpace(dbConnection.Username))
    throw new InvalidOperationException("Migration connection requires PostgreSQL host and username.");
if (resolvedConfiguration.Source == "config-server" &&
    !string.Equals(dbConnection.Host, resolvedConfiguration.ServerHost, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Config Server connection host does not match the registered migration target server.");

Console.WriteLine($"Migration configuration: {resolvedConfiguration.Source}; deployment: {resolvedConfiguration.Deployment}; target: {target}; server host: {dbConnection.Host}; database: {expectedDatabase}");

var options = new DbContextOptionsBuilder<SemanticPostgresMigrationContext>()
    .UseNpgsql(dbConnection.ConnectionString, provider => provider.EnableRetryOnFailure())
    .Options;
await using var context = new SemanticPostgresMigrationContext(options);
var databaseCreator = context.GetService<IRelationalDatabaseCreator>();
var exists = await databaseCreator.ExistsAsync();

Console.WriteLine($"Target: semantic-{target}; action: {action}; database state: {(exists ? "exists" : "missing")}");
if (!exists && action == "status")
{
    Console.WriteLine("MISSING: target database is authorized but not yet present; applying this image will create it.");
    return;
}

if (!exists)
{
    // Dedicated bootstrap credentials may be provided for CREATE DATABASE.
    // Otherwise, the migration credential itself must have permission to create
    // the database on the existing server.
    await CreateDatabaseIfMissingAsync(dbConnection, expectedDatabase, resolvedConfiguration.BootstrapConnectionString);
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
    Console.WriteLine(pending.Length == 0 ? "CURRENT: no migrations required." : $"BEHIND: {pending.Length} migration(s) required.");
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

// Verify the actual schema mappings and SQL view. SELECT against an empty
// table still exposes missing objects or columns.
await context.Registry.AsNoTracking().Take(1).ToListAsync();
await context.Subjects.AsNoTracking().Take(1).ToListAsync();
await context.Concepts.AsNoTracking().Take(1).ToListAsync();
await context.Definitions.AsNoTracking().Take(1).ToListAsync();
await context.DefinitionView.AsNoTracking().Take(1).ToListAsync();
await context.Embeddings.AsNoTracking().Take(1).ToListAsync();
await context.Artifacts.AsNoTracking().Take(1).ToListAsync();
await context.ArtifactInformationElements.AsNoTracking().Take(1).ToListAsync();
Console.WriteLine($"VERIFIED: schema revision {known[^1]} and seven semantic tables plus view.");

static async Task<bool> ContainsExistingSemanticTablesAsync(string connectionString)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var cmd = new NpgsqlCommand(
        "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='dbo' AND table_name = ANY (ARRAY['registry','subjects','concepts','definitions','embeddings','artifacts','artifactinformationelements']))",
        connection);
    return (bool)(await cmd.ExecuteScalarAsync() ?? false);
}

static async Task CreateDatabaseIfMissingAsync(NpgsqlConnectionStringBuilder target, string databaseName, string? bootstrapRaw)
{
    var bootstrap = string.IsNullOrWhiteSpace(bootstrapRaw)
        ? new NpgsqlConnectionStringBuilder(target.ConnectionString) { Database = "postgres" }
        : new NpgsqlConnectionStringBuilder(bootstrapRaw);

    if (!string.Equals(bootstrap.Host, target.Host, StringComparison.OrdinalIgnoreCase) ||
        bootstrap.Port != target.Port ||
        string.Equals(bootstrap.Database, databaseName, StringComparison.Ordinal))
        throw new InvalidOperationException("Bootstrap credentials must address the same server through a different maintenance database.");

    await using var connection = new NpgsqlConnection(bootstrap.ConnectionString);
    await connection.OpenAsync();

    // The maintenance database provides a lock even before the destination DB
    // exists. Serializing creation avoids two Jobs racing to CREATE DATABASE.
    await using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_lock(hashtext(@name))", connection);
    lockCmd.Parameters.AddWithValue("name", "nuviot:semantic-schema-migrator:" + databaseName);
    await lockCmd.ExecuteNonQueryAsync();
    try
    {
        await using var existsCmd = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname=@database)", connection);
        existsCmd.Parameters.AddWithValue("database", databaseName);
        if ((bool)(await existsCmd.ExecuteScalarAsync() ?? false))
            return;

        // Identifiers cannot be parameterized; names are validated above to a
        // finite image-owned allowlist, and are still correctly quoted.
        var quotedDatabase = "\"" + databaseName.Replace("\"", "\"\"") + "\"";
        var quotedOwner = "\"" + target.Username.Replace("\"", "\"\"") + "\"";
        await using var createCmd = new NpgsqlCommand($"CREATE DATABASE {quotedDatabase} OWNER {quotedOwner}", connection);
        try
        {
            await createCmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42501")
        {
            throw new InvalidOperationException(
                "Target database is missing and the supplied bootstrap role cannot CREATE DATABASE. Provide an authorized bootstrap Secret.", ex);
        }
    }
    finally
    {
        await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtext(@name))", connection);
        unlockCmd.Parameters.AddWithValue("name", "nuviot:semantic-schema-migrator:" + databaseName);
        await unlockCmd.ExecuteNonQueryAsync();
    }
}
