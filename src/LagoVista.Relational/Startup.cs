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

            // Each logical registry can select a provider independently. SQL Server stays the default
            // for existing installations; no schema migrations run as part of application startup.
            var liveSection = configurationRoot.GetSection("SemanticDb");
            var testSection = configurationRoot.GetSection("SemanticTestDb");
            var livePostgres = IsPostgres(liveSection);
            var testPostgres = IsPostgres(testSection);
            var liveConnectionString = BuildSemanticConnectionString(liveSection, livePostgres);
            var testConnectionString = BuildSemanticConnectionString(testSection, testPostgres);

            services.AddDbContext<SemanticDataContext>(options => UseSemanticProvider(options, livePostgres, liveConnectionString), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticDataContext>(options =>
            {
                UseSemanticProvider(options, livePostgres, liveConnectionString);
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
            });

            services.AddDbContext<SemanticTestDataContext>(options => UseSemanticProvider(options, testPostgres, testConnectionString), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticTestDataContext>(options =>
            {
                UseSemanticProvider(options, testPostgres, testConnectionString);
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
            });
        }

        private static bool IsPostgres(IConfigurationSection section)
        {
            var provider = section["Provider"];
            if (string.IsNullOrWhiteSpace(provider) ||
                string.Equals(provider, "SqlServer", System.StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(provider, "Postgres", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(provider, "PostgreSQL", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(provider, "Npgsql", System.StringComparison.OrdinalIgnoreCase))
                return true;
            throw new System.InvalidOperationException($"Unsupported semantic provider '{provider}' for '{section.Path}'.");
        }

        private static string BuildSemanticConnectionString(IConfigurationSection section, bool postgres)
        {
            if (!postgres) return BuildSqlConnectionString(CreateConnectionSettings(section));
            var portText = section["Port"];
            var port = string.IsNullOrWhiteSpace(portText) ? 5432 : int.Parse(portText, System.Globalization.CultureInfo.InvariantCulture);
            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = section.Require("HostName"),
                Port = port,
                Database = section.Require("InitialCatalog"),
                Username = section.Require("UserName"),
                Password = section.Require("Password"),
                Timeout = 15,
                CommandTimeout = 60
            };
            return builder.ConnectionString;
        }

        private static void UseSemanticProvider(DbContextOptionsBuilder options, bool postgres, string connectionString)
        {
            if (postgres)
                options.UseNpgsql(connectionString, provider => provider.EnableRetryOnFailure());
            else
                options.UseSqlServer(connectionString, provider => provider.EnableRetryOnFailure());
        }

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
