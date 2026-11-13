using LagoVista.Relational.DataContexts;
using LagoVista.Relational.MigrationRuntime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DatabaseMigration.Tests;

[TestClass]
public sealed class MigrationConfigurationTests
{
    private static IConfiguration Config(
        string provider = "postgresql",
        string database = "semantic_production",
        string host = "postgres-rw",
        string? connection = "Host=postgres-rw;Database=semantic_production;Username=migrator;Password=not-a-real-password")
    {
        return new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DatabaseMigration:Targets:production:Provider"] = provider,
                ["DatabaseMigration:Targets:production:Database"] = database,
                ["DatabaseMigration:Targets:production:ServerHost"] = host,
                ["DatabaseMigration:Targets:production:ConnectionString"] = connection,
                ["DatabaseMigration:Targets:production:BootstrapConnectionString"] = "Host=postgres-rw;Database=postgres;Username=bootstrap;Password=not-a-real-password"
            }).Build();
    }

    [TestMethod]
    public void Valid_config_uses_approved_database_identity()
    {
        var resolved = MigrationConfigurationLoader.ResolveRemoteTarget(Config(),
            "production", "semantic_production", "postgresql", "semantic-schema-migrator", "dev");

        Assert.AreEqual("config-server", resolved.Source);
        Assert.AreEqual("semantic_production", resolved.Database);
        Assert.AreEqual("postgres-rw", resolved.ServerHost);
        Assert.AreEqual("semantic-schema-migrator", resolved.AppKey);
        Assert.IsTrue(resolved.ConnectionString.Contains("Username=migrator"));
        Assert.IsNotNull(resolved.BootstrapConnectionString);
        Assert.IsFalse(resolved.ToString().Contains("not-a-real-password", StringComparison.Ordinal),
            "Record ToString must not disclose credentials.");
        var json = System.Text.Json.JsonSerializer.Serialize(resolved);
        Assert.IsFalse(json.Contains("not-a-real-password", StringComparison.Ordinal),
            "Serialized operation views must not disclose credentials.");
        Assert.IsFalse(json.Contains("ConnectionString", StringComparison.Ordinal),
            "Serialized operation views must not include secret property names.");

    }

    [TestMethod]
    public void Missing_database_in_server_is_not_a_configuration_error()
    {
        // No connection or server probe occurs during Config Server target
        // resolution; a missing physical database is a supported job start state.
        var resolved = MigrationConfigurationLoader.ResolveRemoteTarget(Config(),
            "production", "semantic_production", "postgresql", "semantic-schema-migrator", "dev");
        Assert.AreEqual("semantic_production", resolved.Database);
    }

    [TestMethod]
    public void Wrong_database_is_rejected()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(database: "other"),
                "production", "semantic_production", "postgresql",
                "semantic-schema-migrator", "dev"));
    }

    [TestMethod]
    public void Wrong_provider_is_rejected()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(provider: "azure-sql"),
                "production", "semantic_production", "postgresql",
                "semantic-schema-migrator", "dev"));
    }

    [TestMethod]
    public void Missing_server_identity_is_rejected()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(host: ""),
                "production", "semantic_production", "postgresql",
                "semantic-schema-migrator", "dev"));
    }

    [TestMethod]
    public void Missing_connection_is_rejected_without_direct_fallback()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(connection: null),
                "production", "semantic_production", "postgresql",
                "semantic-schema-migrator", "dev"));
    }

    [TestMethod]
    public void Unknown_deployment_is_rejected()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(),
                "production", "semantic_production", "postgresql",
                "semantic-schema-migrator", "sandbox"));
    }
}

[TestClass]
public sealed class SemanticModelDriftTests
{
    [TestMethod]
    public void PostgreSql_semantic_model_matches_checked_in_snapshot()
    {
        // EF Core compares the compiled model to the checked-in snapshot;
        // no database or Config Server connection is required.
        var options = new DbContextOptionsBuilder<SemanticPostgresMigrationContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=semantic_test;Username=unused;Password=unused")
            .Options;
        using var context = new SemanticPostgresMigrationContext(options);
        var drift = context.Database.HasPendingModelChanges();
        if (drift)
        {
            var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
            var current = context.GetService<IDesignTimeModel>().Model;
            var changes = snapshot is null
                ? "No compiled migration snapshot was found."
                : string.Join("; ", context.GetService<IMigrationsModelDiffer>()
                    .GetDifferences(snapshot.GetRelationalModel(), current.GetRelationalModel())
                    .Select(op => op switch
                    {
                        AlterColumnOperation x => $"AlterColumn {x.Schema}.{x.Table}.{x.Name} {x.ColumnType}",
                        AddColumnOperation x => $"AddColumn {x.Schema}.{x.Table}.{x.Name} {x.ColumnType}",
                        DropColumnOperation x => $"DropColumn {x.Schema}.{x.Table}.{x.Name}",
                        CreateIndexOperation x => $"CreateIndex {x.Schema}.{x.Table}.{x.Name}",
                        DropIndexOperation x => $"DropIndex {x.Schema}.{x.Table}.{x.Name}",
                        DropPrimaryKeyOperation x => $"DropPrimaryKey {x.Schema}.{x.Table}.{x.Name}",
                        AddPrimaryKeyOperation x => $"AddPrimaryKey {x.Schema}.{x.Table}.{x.Name}",
                        DropForeignKeyOperation x => $"DropForeignKey {x.Schema}.{x.Table}.{x.Name}",
                        AddForeignKeyOperation x => $"AddForeignKey {x.Schema}.{x.Table}.{x.Name}",
                        CreateTableOperation x => $"CreateTable {x.Schema}.{x.Name}",
                        _ => op.GetType().Name
                    }).Take(30));
            Assert.Fail("The semantic EF model differs from its PostgreSQL migration snapshot: " + changes);
        }
    }
}
