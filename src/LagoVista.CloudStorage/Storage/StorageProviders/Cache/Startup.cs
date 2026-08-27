using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cache
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICacheProvider, CacheProvider>();

            services.AddScoped<EntityListItemCache>();
            services.AddScoped<IEntityListItemCache>(provider => provider.GetRequiredService<EntityListItemCache>());
            services.AddScoped<IEntityListCacheInvalidator>(provider => provider.GetRequiredService<EntityListItemCache>());
        }
    }
}
