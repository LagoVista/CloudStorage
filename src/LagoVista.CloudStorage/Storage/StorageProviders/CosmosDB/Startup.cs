using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders.CosmosDB
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICosmosClientProvider>(CosmosClientProvider.Shared);
            services.AddScoped<ICosmosDocumentStorageClient, CosmosDocumentStorageClient>();
        }
    }
}
