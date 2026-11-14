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
    private static IConfigurationRoot BuildConfiguration(bool includeCluster = true)
    {
        var values = new Dictionary<string, string?>
        {
            // Obsolete SQL Server values, including explicit Provider overrides,
            // must never redirect the two PostgreSQL-only InfoElements contexts.
            ["SemanticDb:Provider"] = "SqlServer",
            ["SemanticDb:ServerURL"] = "old-sql.example.org",
            ["SemanticDb:InitialCatalog"] = "legacy_production",
            ["SemanticDb:UserName"] = "old-sql-login",
            ["SemanticDb:Password"] = "old-sql-password",
            ["SemanticTestDb:Provider"] = "SqlServer",
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
    public void Both_semantic_contexts_use_cluster_postgres_regardless_of_obsolete_sql_configuration()
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
    public void Scoped_semantic_contexts_also_use_postgresql()
    {
        using var provider = ConfigureServices(BuildConfiguration());
        using var scope = provider.CreateScope();
        using var production = scope.ServiceProvider.GetRequiredService<SemanticDataContext>();
        using var test = scope.ServiceProvider.GetRequiredService<SemanticTestDataContext>();
        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", production.Database.ProviderName);
        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", test.Database.ProviderName);
    }

    [TestMethod]
    public void Missing_cluster_postgres_configuration_fails_closed_without_sql_server_fallback()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
        {
            using var provider = ConfigureServices(BuildConfiguration(includeCluster: false));
        });
    }

    [TestMethod]
    public void Semantic_design_time_factory_uses_postgresql()
    {
        using var context = new SemanticDataContextDesignTimeFactory().CreateDbContext([]);
        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }
}
