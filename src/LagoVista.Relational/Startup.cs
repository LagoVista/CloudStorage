using LagoVista.Core.Interfaces;
using LagoVista.Core.Interfaces.Crypto;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.Configuration;
using System.Runtime.CompilerServices;

namespace LagoVista.Relational
{
    public static class Startup
    {
        public static void ConfigureDataContextServices(IConfigurationRoot configurationRoot, Microsoft.Extensions.DependencyInjection.IServiceCollection services, ILogger logger)
        {
            var billingDbSection = configurationRoot.GetSection("BillingDb");
            if (billingDbSection == null)
            {
                logger.AddCustomEvent(LogLevel.ConfigurationError, "[BillingManager_Startup]", "Missing Section BillingDb");
                throw new InvalidConfigurationException($"[Relational__Startup] Missing Section BillingDb");
            }

            var connectionSettings = new ConnectionSettings()
            {
                Uri = billingDbSection["ServerURL"],
                ResourceName = billingDbSection["InitialCatalog"],
                UserName = billingDbSection["UserName"],
                Password = billingDbSection["Password"],
            };


            services.AddScoped<IKeyIdTargetResolver, Services.KeyIdTargetResolver>();   

            var connectionString = $"Server=tcp:{connectionSettings.Uri},1433;Initial Catalog={connectionSettings.ResourceName};Persist Security Info=False;User ID={connectionSettings.UserName};Password={connectionSettings.Password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            services.AddDbContext<BillingDataContext>(options => options.UseSqlServer(connectionString, moreOptions => moreOptions.EnableRetryOnFailure()));
        }
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