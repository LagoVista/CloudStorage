using LagoVista.Relational.DataContexts;
using LagoVista.Relational.MigrationRuntime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DatabaseMigration.Tests;

[TestClass]
public sealed class MigrationConfigurationTests
{
    private static IConfiguration Config(
        string? url = "postgresql://postgres-rw:5432",
        string? user = "migrator",
        string? password = "not-a-real-password")
    {
        return new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DbMigrator:PostgresUrl"] = url,
                ["DbMigrator:PostgresUserName"] = user,
                ["DbMigrator:PostgresPassword"] = password
            }).Build();
    }

    [TestMethod]
    public void Exported_manifest_uses_shared_provider_keys_without_values()
    {
        var manifest = MigrationConfigurationContract.ExportConfigManifest();
        var lines = manifest.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(10, lines.Length);
        Assert.AreEqual("BEGIN CONFIG", lines[0]);
        Assert.AreEqual("END CONFIG", lines[^1]);
        foreach (var name in new[] {
            "SqlServerUrl", "SqlServerUserName", "SqlServerPassword",
            "PostgresUrl", "PostgresUserName", "PostgresPassword",
            "PostgresBootstrapUserName", "PostgresBootstrapPassword"
        })
            Assert.IsTrue(lines.Contains($"DbMigrator:{name},false,false"));
        Assert.IsFalse(manifest.Contains("not-a-real-password", StringComparison.Ordinal));
        Assert.IsFalse(manifest.Contains("postgres-rw", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Postgres_configuration_constructs_approved_database_connection_and_redacts_password()
    {
        var resolved = MigrationConfigurationLoader.ResolveRemoteTarget(
            Config(password: "some;pass=word"), "production",
            "semantic_production", "postgresql", "db-migrator", "dev");
        var parsed = new NpgsqlConnectionStringBuilder(resolved.ConnectionString);
        Assert.AreEqual("semantic_production", parsed.Database);
        Assert.AreEqual("postgres-rw", parsed.Host);
        Assert.AreEqual(5432, parsed.Port);
        Assert.AreEqual("migrator", parsed.Username);
        Assert.AreEqual("some;pass=word", parsed.Password);
        Assert.AreEqual("db-migrator", resolved.AppKey);
        Assert.IsNull(resolved.BootstrapConnectionString);
        Assert.IsFalse(resolved.ToString().Contains("some;pass=word", StringComparison.Ordinal));
        var json = System.Text.Json.JsonSerializer.Serialize(resolved);
        Assert.IsFalse(json.Contains("some;pass=word", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("ConnectionString", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Optional_separate_postgres_bootstrap_uses_same_host_and_maintenance_database()
    {
        var values = new Dictionary<string, string?>
        {
            ["DbMigrator:PostgresUrl"] = "postgresql://postgres-rw:5432",
            ["DbMigrator:PostgresUserName"] = "migrator",
            ["DbMigrator:PostgresPassword"] = "migration-password",
            ["DbMigrator:PostgresBootstrapUserName"] = "database-creator",
            ["DbMigrator:PostgresBootstrapPassword"] = "bootstrap;pass=secret"
        };
        var settings = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var resolved = MigrationConfigurationLoader.ResolveRemoteTarget(
            settings, "test", "semantic_test", "postgresql", "db-migrator", "dev");
        var runtime = new NpgsqlConnectionStringBuilder(resolved.ConnectionString);
        var bootstrap = new NpgsqlConnectionStringBuilder(resolved.BootstrapConnectionString);
        Assert.AreEqual(runtime.Host, bootstrap.Host);
        Assert.AreEqual(runtime.Port, bootstrap.Port);
        Assert.AreEqual("postgres", bootstrap.Database);
        Assert.AreEqual("database-creator", bootstrap.Username);
        Assert.AreEqual("bootstrap;pass=secret", bootstrap.Password);
        Assert.AreEqual("migrator", runtime.Username);
        Assert.AreEqual("semantic_test", runtime.Database);
        Assert.IsFalse(resolved.ToString().Contains("bootstrap;pass=secret", StringComparison.Ordinal));
        var json = System.Text.Json.JsonSerializer.Serialize(resolved);
        Assert.IsFalse(json.Contains("bootstrap;pass=secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("migration-password", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Partial_bootstrap_configuration_fails_closed_without_fallback()
    {
        foreach (var missing in new[] { "DbMigrator:PostgresBootstrapUserName", "DbMigrator:PostgresBootstrapPassword" })
        {
            var values = new Dictionary<string, string?>
            {
                ["DbMigrator:PostgresUrl"] = "postgresql://postgres-rw:5432",
                ["DbMigrator:PostgresUserName"] = "migrator",
                ["DbMigrator:PostgresPassword"] = "migration-password",
                ["DbMigrator:PostgresBootstrapUserName"] = "creator",
                ["DbMigrator:PostgresBootstrapPassword"] = "bootstrap-secret"
            };
            values.Remove(missing);
            var settings = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                MigrationConfigurationLoader.ResolveRemoteTarget(
                    settings, "test", "semantic_test", "postgresql", "db-migrator", "dev"));
        }
    }

    [TestMethod]
    public void Missing_database_does_not_prevent_configuration_resolution()
    {
        var resolved = MigrationConfigurationLoader.ResolveRemoteTarget(
            Config(), "test", "semantic_test", "postgresql", "db-migrator", "dev");
        Assert.AreEqual("semantic_test", new NpgsqlConnectionStringBuilder(resolved.ConnectionString).Database);
    }

    [TestMethod]
    public void Unsafe_postgres_url_and_missing_credentials_are_rejected()
    {
        foreach (var url in new[] {
            "postgresql://other/schema", "postgresql://user:pass@other",
            "postgresql://other?sslmode=disable", "postgresql://other/#x"
        })
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                MigrationConfigurationLoader.ResolveRemoteTarget(Config(url: url),
                    "production", "semantic_production", "postgresql", "db-migrator", "dev"));
        }
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(password: null),
                "production", "semantic_production", "postgresql", "db-migrator", "dev"));
    }

    [TestMethod]
    public void Wrong_app_and_deployment_are_rejected()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(), "production",
                "semantic_production", "postgresql", "semantic-schema-migrator", "dev"));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(), "production",
                "semantic_production", "postgresql", "db-migrator", "sandbox"));
    }

    [TestMethod]
    public void Postgres_host_only_uses_the_default_port()
    {
        var resolved = MigrationConfigurationLoader.ResolveRemoteTarget(
            Config(url: "nuviot-postgres-rw"), "test",
            "semantic_test", "postgresql", "db-migrator", "dev");
        var parsed = new NpgsqlConnectionStringBuilder(resolved.ConnectionString);
        Assert.AreEqual("nuviot-postgres-rw", parsed.Host);
        Assert.AreEqual(5432, parsed.Port);
        Assert.AreEqual("semantic_test", parsed.Database);
    }

    [TestMethod]
    public void Missing_postgres_endpoint_or_username_is_rejected()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(url: null),
                "production", "semantic_production", "postgresql", "db-migrator", "dev"));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(user: null),
                "production", "semantic_production", "postgresql", "db-migrator", "dev"));
    }

    [TestMethod]
    public void Invalid_target_database_or_provider_cannot_select_new_credentials()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(), "production",
                "another;Password=unexpected", "postgresql", "db-migrator", "dev"));
        Assert.ThrowsExactly<ArgumentException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(Config(), "production",
                "semantic_production", "other-provider", "db-migrator", "dev"));
    }

    [TestMethod]
    public void Sql_server_rejects_untrusted_endpoint_and_missing_credentials()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DbMigrator:SqlServerUrl"] = "sql.example.net;TrustServerCertificate=true",
                ["DbMigrator:SqlServerUserName"] = "migration-user",
                ["DbMigrator:SqlServerPassword"] = "not-a-real-password"
            }).Build();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MigrationConfigurationLoader.ResolveRemoteTarget(config, "schema",
                "sample_db", "azure-sql", "db-migrator", "dev"));
    }

    [TestMethod]
    public void Sql_server_configuration_is_provider_specific()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DbMigrator:SqlServerUrl"] = "tcp:sql.example.net,1433",
                ["DbMigrator:SqlServerUserName"] = "migration-user",
                ["DbMigrator:SqlServerPassword"] = "not-a-real-password"
            }).Build();
        var resolved = MigrationConfigurationLoader.ResolveRemoteTarget(config, "schema",
            "sample_db", "azure-sql", "db-migrator", "dev");
        Assert.AreEqual("tcp:sql.example.net,1433", resolved.ServerHost);
        Assert.IsTrue(resolved.ConnectionString.Contains("sample_db", StringComparison.Ordinal));
        Assert.IsFalse(resolved.ToString().Contains("not-a-real-password", StringComparison.Ordinal));
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
