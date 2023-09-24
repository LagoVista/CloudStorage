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
        }
    }
}
