using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMongoStorageClientFactory, MongoStorageClientFactory>();
            services.AddSingleton<IApplicationDataStore, MongoApplicationDataStore>();
            services.AddSingleton<IScratchStore, MongoScratchStore>();
            services.AddScoped<IMongoDocumentStorageClient, MongoDocumentStorageClient>();
        }
    }
}
