using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.StorageProviders;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            Mongo.Startup.ConfigureServices(services);
            CosmosDB.Startup.ConfigureServices(services);
            
            services.AddTransient<IDocumentStorageClientProvider, DocumentStorageClientProvider>();
        }
    }
}
