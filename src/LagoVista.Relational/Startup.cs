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

            var liveConnectionSettings = CreateConnectionSettings(configurationRoot.GetSection("SemanticDb"));
            var testConnectionSettings = CreateConnectionSettings(configurationRoot.GetSection("SemanticTestDb"));

            var liveConnectionString = BuildSqlConnectionString(liveConnectionSettings);
            var testConnectionString = BuildSqlConnectionString(testConnectionSettings);

            services.AddDbContext<SemanticDataContext>(options => options.UseSqlServer(liveConnectionString, moreOptions => moreOptions.EnableRetryOnFailure()), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticDataContext>(options =>
            {
                options.UseSqlServer(liveConnectionString, moreOptions => moreOptions.EnableRetryOnFailure());
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
                options.LogTo(msg => { }, Microsoft.Extensions.Logging.LogLevel.Information);
            });

            services.AddDbContext<SemanticTestDataContext>(options => options.UseSqlServer(testConnectionString, moreOptions => moreOptions.EnableRetryOnFailure()), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticTestDataContext>(options =>
            {
                options.UseSqlServer(testConnectionString, moreOptions => moreOptions.EnableRetryOnFailure());
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
                options.LogTo(msg => { }, Microsoft.Extensions.Logging.LogLevel.Information);
            });
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
