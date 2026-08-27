using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders.AzureTable
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<INodeLocatorTableWriterBatched, NodeLocatorTableWriterBatched>();
            services.AddScoped<INodeLocatorTableReader, NodeLocatorTableReader>();
            services.AddScoped<IFkIndexTableWriterBatched, FkIndexTableWriterBatched>();
        }
    }
}
