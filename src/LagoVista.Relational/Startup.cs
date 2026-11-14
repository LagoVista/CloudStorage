using LagoVista.CloudStorage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Interfaces.Crypto;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Relational.DataContexts;
using LagoVista.Relational.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LagoVista.Relational
{
    public static class Startup
    {
        private static void ConfigurePlatformSmokeTests(IServiceCollection services)
        {
            services.TryAddSingleton<IPostgresConnectionSettings, PostgresConnectionSettings>();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPlatformSmokeTest, PostgresPlatformSmokeTest>());
        }

        public static void ConfigureDataContextServices(IConfigurationRoot configurationRoot, IServiceCollection services, ILogger logger)
        {
            ConfigurePlatformSmokeTests(services);

            var section = configurationRoot.GetSection("BillingDb");
            var connectionSettings = CreateConnectionSettings(section);

            services.AddSingleton<ICacheProviderSettings, CacheProviderSettings>();
            services.AddScoped<IKeyIdTargetResolver, Services.KeyIdTargetResolver>();

            var connectionString = BuildSqlConnectionString(connectionSettings);

            services.AddDbContext<BillingDataContext>(options => options.UseSqlServer(connectionString, moreOptions => moreOptions.EnableRetryOnFailure()), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<BillingDataContext>(options =>
            {
                options.UseSqlServer(connectionString, moreOptions => moreOptions.EnableRetryOnFailure());
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
                options.LogTo(msg => { }, Microsoft.Extensions.Logging.LogLevel.Information);
            });
        }

        public static void ConfigureSemanticDataContextServices(IConfigurationRoot configurationRoot, IServiceCollection services, ILogger logger)
        {
            ConfigurePlatformSmokeTests(services);

            // InfoElements has no SQL Server runtime or provider cutover flag.
            // The existing ClusterPostgres configuration owns the shared endpoint
            // and login; the two semantic contexts own their fixed database names.
            // Schema changes remain exclusively the migration container's job.
            var clusterPostgres = new PostgresConnectionSettings(configurationRoot);
            var liveConnectionString = BuildSemanticPostgresConnectionString(clusterPostgres, "semantic_production");
            var testConnectionString = BuildSemanticPostgresConnectionString(clusterPostgres, "semantic_test");

            services.AddDbContext<SemanticDataContext>(options => UseSemanticPostgres(options, liveConnectionString), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticDataContext>(options =>
            {
                UseSemanticPostgres(options, liveConnectionString);
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
            });

            services.AddDbContext<SemanticTestDataContext>(options => UseSemanticPostgres(options, testConnectionString), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticTestDataContext>(options =>
            {
                UseSemanticPostgres(options, testConnectionString);
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
            });
        }

        private static string BuildSemanticPostgresConnectionString(
            IPostgresConnectionSettings clusterPostgres, string database)
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = clusterPostgres.HostName,
                Port = clusterPostgres.Port,
                Database = database,
                Username = clusterPostgres.UserName,
                Password = clusterPostgres.Password,
                Timeout = 15,
                CommandTimeout = 60
            };
            return builder.ConnectionString;
        }

        private static void UseSemanticPostgres(DbContextOptionsBuilder options, string connectionString) =>
            options.UseNpgsql(connectionString, provider => provider.EnableRetryOnFailure());

        private static ConnectionSettings CreateConnectionSettings(IConfigurationSection section) => new ConnectionSettings()
        {
            Uri = section.Require("ServerURL"),
            ResourceName = section.Require("InitialCatalog"),
            UserName = section.Require("UserName"),
            Password = section.Require("Password"),
        };

        private static string BuildSqlConnectionString(ConnectionSettings connectionSettings) => $"Server=tcp:{connectionSettings.Uri},1433;Initial Catalog={connectionSettings.ResourceName};Persist Security Info=False;User ID={connectionSettings.UserName};Password={connectionSettings.Password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    }
}

namespace LagoVista.DependencyInjection
{
    public static class RelationalStorageModule
    {
        public static void AddRelationalStorageModule(this IServiceCollection services, IConfigurationRoot configurationRoot, ILogger logger)
        {
            LagoVista.Relational.Startup.ConfigureDataContextServices(configurationRoot, services, logger);
        }

        public static void AddSemanticRelationalStorageModule(this IServiceCollection services, IConfigurationRoot configurationRoot, ILogger logger)
        {
            LagoVista.Relational.Startup.ConfigureSemanticDataContextServices(configurationRoot, services, logger);
        }
    }
}
