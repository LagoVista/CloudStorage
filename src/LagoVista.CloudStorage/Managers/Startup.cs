using LagoVista.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Managers
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ICategoryManager, CategoryManager>();
            services.AddScoped<IEntityListResponseMetadataProvider, EntityListResponseMetadataProvider>();
            services.AddScoped<IEntityListItemManager, EntityListItemManager>();
        }
    }
}
