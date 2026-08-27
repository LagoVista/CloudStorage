using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Utils.TableSizer;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Utils
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IStorageUtils, StorageUtils>();
            services.AddScoped<ITableSizer, TableSizer>();
        }
    }
}
