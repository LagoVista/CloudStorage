// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: fb6351dbc21fb32befd31cbcdb2f36abeb3a9628a825d24fd89d091ede688782
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Managers;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;

namespace LagoVista.CloudStorage
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddSingleton<IStorageUtils, StorageUtils>();
            services.AddSingleton<ICategoryManager, CategoryManager>();
        }
    }
}
