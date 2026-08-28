using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.File;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            AzureTable.Startup.ConfigureServices(services);
            Cassandra.Startup.ConfigureServices(services);
            Mongo.Startup.ConfigureServices(services);
            CosmosDB.Startup.ConfigureServices(services);
            Cache.Startup.ConfigureServices(services);


            services.AddScoped<ICloudFileStorageClient, S3CloudFileStorageClient>();
            services.AddScoped<IDocumentCloudServices, DocumentCloudServices>();
            services.AddScoped<IDocumentCloudCachedServices, DocumentCloudCachedServices>();
            services.AddScoped<IDocumentCollectionNameResolver, DocumentCollectionNameResolver>();
            services.AddScoped<IDocumentStorageClientProvider, DocumentStorageClientProvider>();
        }
    }
}
