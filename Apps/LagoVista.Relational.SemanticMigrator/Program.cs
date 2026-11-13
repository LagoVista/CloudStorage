using System;
using System.Linq;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;

var connection = Environment.GetEnvironmentVariable("MIGRATION_CONNECTION_STRING");
var action = Environment.GetEnvironmentVariable("MIGRATION_ACTION") ?? "status";
var target = Environment.GetEnvironmentVariable("MIGRATION_TARGET") ?? "unknown";

if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("MIGRATION_CONNECTION_STRING must be provided through a Kubernetes Secret.");

if (!string.Equals(action, "status", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(action, "apply", StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("MIGRATION_ACTION must be either 'status' or 'apply'.");

var options = new DbContextOptionsBuilder<SemanticPostgresMigrationContext>()
    .UseNpgsql(connection, provider => provider.EnableRetryOnFailure())
    .Options;
await using var context = new SemanticPostgresMigrationContext(options);

var applied = await context.Database.GetAppliedMigrationsAsync();
var pending = await context.Database.GetPendingMigrationsAsync();
Console.WriteLine($"Semantic PostgreSQL schema target: {target}; action: {action}");
Console.WriteLine($"Applied migration count: {applied.Count()}; pending: {pending.Count()}");
foreach (var migration in pending) Console.WriteLine($"PENDING {migration}");
if (string.Equals(action, "apply", StringComparison.OrdinalIgnoreCase))
{
    await context.Database.MigrateAsync();
    // Verify every mapping, including the SQL view, against the actual database
    // after applying migrations. Even empty tables reveal missing columns in SELECT.
    await context.Registry.AsNoTracking().Take(1).ToListAsync();
    await context.Subjects.AsNoTracking().Take(1).ToListAsync();
    await context.Concepts.AsNoTracking().Take(1).ToListAsync();
    await context.Definitions.AsNoTracking().Take(1).ToListAsync();
    await context.DefinitionView.AsNoTracking().Take(1).ToListAsync();
    await context.Embeddings.AsNoTracking().Take(1).ToListAsync();
    await context.Artifacts.AsNoTracking().Take(1).ToListAsync();
    await context.ArtifactInformationElements.AsNoTracking().Take(1).ToListAsync();
    Console.WriteLine("Verified seven semantic tables and the semantic definition view.");
    Console.WriteLine("Semantic PostgreSQL migrations applied successfully.");
}
