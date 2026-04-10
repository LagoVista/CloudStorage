// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: fb6351dbc21fb32befd31cbcdb2f36abeb3a9628a825d24fd89d091ede688782
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Managers;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.CloudStorage.Utils.TableSizer;
using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IStorageUtils, StorageUtils>();
            services.AddScoped<IDocumentCloudServices, DocumentCloudServices>();
            services.AddScoped<IDocumentCloudCachedServices, DocumentCloudCachedServices>();
            services.AddScoped<ICategoryManager, CategoryManager>();
            services.AddScoped<ITableSizer, TableSizer>();
            services.AddScoped<INodeLocatorTableWriterBatched,  NodeLocatorTableWriterBatched>();
            services.AddScoped<ISyncRepository, CosmosSyncRepository>();
            services.AddScoped<INodeLocatorTableReader, NodeLocatorTableReader>();
            services.AddScoped<IFkIndexTableWriterBatched, FkIndexTableWriterBatched>();

            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddSingleton<ISyncConnectionSettings, SyncConnections>();
            services.AddSingleton<IDefaultConnectionSettings, DefaultConnectionSettings>();

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