// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: fb6351dbc21fb32befd31cbcdb2f36abeb3a9628a825d24fd89d091ede688782
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Managers;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Interfaces;

namespace LagoVista.CloudStorage
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddSingleton<IStorageUtils, StorageUtils>();
            services.AddSingleton<IDocumentCloudServices, DocumentCloudServices>();
            services.AddSingleton<IDocumentCloudCachedServices, DocumentCloudCachedServices>();
            services.AddSingleton<ICategoryManager, CategoryManager>();
            services.AddSingleton<ISyncRepository, CosmosSyncRepository>();
        }
    }
}
