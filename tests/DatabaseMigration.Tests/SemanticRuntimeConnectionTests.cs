using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DatabaseMigration.Tests;

[TestClass]
public sealed class SemanticRuntimeConnectionTests
{
    private static IConfigurationRoot BuildConfiguration(
        string? productionProvider = "Postgres",
        string? testProvider = "Postgres",
        bool includeCluster = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["SemanticDb:Provider"] = productionProvider,
            ["SemanticTestDb:Provider"] = testProvider,
            // SQL Server configuration is deliberately present: switching
            // providers must not reuse its obsolete database or credentials.
            ["SemanticDb:ServerURL"] = "old-sql.example.org",
            ["SemanticDb:InitialCatalog"] = "legacy_production",
            ["SemanticDb:UserName"] = "old-sql-login",
            ["SemanticDb:Password"] = "old-sql-password",
            ["SemanticTestDb:ServerURL"] = "old-sql.example.org",
            ["SemanticTestDb:InitialCatalog"] = "legacy_test",
            ["SemanticTestDb:UserName"] = "old-sql-login",
            ["SemanticTestDb:Password"] = "old-sql-password"
        };
        if (includeCluster)
        {
            values["ClusterPostgres:HostName"] = "dev-postgres.internal";
            values["ClusterPostgres:Port"] = "5432";
            values["ClusterPostgres:UserName"] = "dev-runtime";
            values["ClusterPostgres:Password"] = "test;secret=with separators";
            values["ClusterPostgres:SchemaName"] = "public";
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ServiceProvider ConfigureServices(IConfigurationRoot config)
    {
        var services = new ServiceCollection();
        Startup.ConfigureSemanticDataContextServices(config, services, null!);
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public void Both_semantic_contexts_use_the_shared_cluster_postgres_identity_and_distinct_databases()
    {
        using var provider = ConfigureServices(BuildConfiguration());
        using var production = provider.GetRequiredService<IDbContextFactory<SemanticDataContext>>().CreateDbContext();
        using var test = provider.GetRequiredService<IDbContextFactory<SemanticTestDataContext>>().CreateDbContext();

        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", production.Database.ProviderName);
        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", test.Database.ProviderName);

        var productionConnection = new NpgsqlConnectionStringBuilder(production.Database.GetDbConnection().ConnectionString);
        var testConnection = new NpgsqlConnectionStringBuilder(test.Database.GetDbConnection().ConnectionString);
        Assert.AreEqual("semantic_production", productionConnection.Database);
        Assert.AreEqual("semantic_test", testConnection.Database);
        foreach (var actual in new[] { productionConnection, testConnection })
        {
            Assert.AreEqual("dev-postgres.internal", actual.Host);
            Assert.AreEqual(5432, actual.Port);
            Assert.AreEqual("dev-runtime", actual.Username);
            Assert.AreEqual("test;secret=with separators", actual.Password);
        }
    }

    [TestMethod]
    public void Opting_in_test_only_keeps_authoritative_semantic_context_on_sql_server()
    {
        using var provider = ConfigureServices(BuildConfiguration(productionProvider: null, testProvider: "Postgres"));
        using var production = provider.GetRequiredService<IDbContextFactory<SemanticDataContext>>().CreateDbContext();
        using var test = provider.GetRequiredService<IDbContextFactory<SemanticTestDataContext>>().CreateDbContext();

        Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", production.Database.ProviderName);
        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", test.Database.ProviderName);
        Assert.AreEqual("semantic_test", new NpgsqlConnectionStringBuilder(test.Database.GetDbConnection().ConnectionString).Database);
    }

    [TestMethod]
    public void Existing_sql_server_configuration_does_not_require_cluster_postgres_settings()
    {
        using var provider = ConfigureServices(BuildConfiguration(productionProvider: null, testProvider: null, includeCluster: false));
        using var production = provider.GetRequiredService<IDbContextFactory<SemanticDataContext>>().CreateDbContext();
        using var test = provider.GetRequiredService<IDbContextFactory<SemanticTestDataContext>>().CreateDbContext();

        Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", production.Database.ProviderName);
        Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", test.Database.ProviderName);
    }
}
