using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Managers;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            Storage.ConnectionSettings.Startup.ConfigureServices(services);
            Storage.StorageProviders.Startup.ConfigureServices(services);
            Repositories.Startup.ConfigureServices(services);
            Utils.Startup.ConfigureServices(services);
            Diagnostics.Startup.ConfigureServices(services);

            services.AddScoped<IDocumentMigrationService, DocumentMigrationService>();

            services.AddScoped<ICategoryManager, CategoryManager>();
            services.AddScoped<IEntityListResponseMetadataProvider, EntityListResponseMetadataProvider>();
            services.AddScoped<IEntityListItemManager, EntityListItemManager>();

            LagoVista.Core.AutoMapper.Startup.ConfigureServices(services);
        }
    }
}

namespace LagoVista.DependencyInjection
{
    public static class CloudStorageModule
    {
        public static void AddCloudStorageModule(this IServiceCollection services, IConfigurationRoot configurationRoot, ILogger logger)
        {
            LagoVista.CloudStorage.Startup.ConfigureServices(services);
        }
    }
}
