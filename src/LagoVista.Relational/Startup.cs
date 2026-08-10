using LagoVista.CloudStorage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Interfaces.Crypto;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LagoVista.Relational
{
    public static class Startup
    {
        public static void ConfigureDataContextServices(IConfigurationRoot configurationRoot, IServiceCollection services, ILogger logger)
        {
            var billingSection = configurationRoot.GetSection("BillingDb");
            var billingConnectionSettings = new ConnectionSettings()
            {
                Uri = billingSection.Require("ServerURL"),
                ResourceName = billingSection.Require("InitialCatalog"),
                UserName = billingSection.Require("UserName"),
                Password = billingSection.Require("Password"),
            };

            var semanticSection = configurationRoot.GetSection("SemanticDb");
            var semanticConnectionSettings = new ConnectionSettings()
            {
                Uri = semanticSection.Require("ServerURL"),
                ResourceName = semanticSection.Require("InitialCatalog"),
                UserName = semanticSection.Require("UserName"),
                Password = semanticSection.Require("Password"),
            };

            var semanticTestSection = configurationRoot.GetSection("SemanticTestDb");
            var semanticTestConnectionSettings = new ConnectionSettings()
            {
                Uri = semanticTestSection.Require("ServerURL"),
                ResourceName = semanticTestSection.Require("InitialCatalog"),
                UserName = semanticTestSection.Require("UserName"),
                Password = semanticTestSection.Require("Password"),
            };

            services.AddSingleton<ICacheProviderSettings, CacheProviderSettings>();
            services.AddScoped<IKeyIdTargetResolver, Services.KeyIdTargetResolver>();

            var billingConnectionString = BuildSqlConnectionString(billingConnectionSettings);
            var semanticConnectionString = BuildSqlConnectionString(semanticConnectionSettings);
            var semanticTestConnectionString = BuildSqlConnectionString(semanticTestConnectionSettings);

            services.AddDbContext<BillingDataContext>(options => options.UseSqlServer(billingConnectionString, moreOptions => moreOptions.EnableRetryOnFailure()), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<BillingDataContext>(options =>
            {
                options.UseSqlServer(billingConnectionString, moreOptions => moreOptions.EnableRetryOnFailure());
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
                options.LogTo(msg => { }, Microsoft.Extensions.Logging.LogLevel.Information);
            });

            services.AddDbContext<SemanticDataContext>(options => options.UseSqlServer(semanticConnectionString, moreOptions => moreOptions.EnableRetryOnFailure()), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticDataContext>(options =>
            {
                options.UseSqlServer(semanticConnectionString, moreOptions => moreOptions.EnableRetryOnFailure());
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
                options.LogTo(msg => { }, Microsoft.Extensions.Logging.LogLevel.Information);
            });

            services.AddDbContext<SemanticTestDataContext>(options => options.UseSqlServer(semanticTestConnectionString, moreOptions => moreOptions.EnableRetryOnFailure()), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            services.AddDbContextFactory<SemanticTestDataContext>(options =>
            {
                options.UseSqlServer(semanticTestConnectionString, moreOptions => moreOptions.EnableRetryOnFailure());
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
                options.LogTo(msg => { }, Microsoft.Extensions.Logging.LogLevel.Information);
            });
        }

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
    }
}
