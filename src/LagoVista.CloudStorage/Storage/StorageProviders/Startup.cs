using LagoVista.CloudStorage.Diagnostics;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.File;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            services.ConfigureScratchData<ApplicationErrorScratchRecord>(definition =>
            {
                definition.Index(x => x.Record.TimeStamp);
                definition.Index(x => x.Record.Application);
                definition.Index(x => x.Record.Environment);
                definition.RetainFor(TimeSpan.FromDays(30));
            });

            services.AddSingleton<IApplicationErrorRepository, ScratchApplicationErrorRepository>();

            services.AddScoped<ICloudFileStorageClient, S3CloudFileStorageClient>();
            services.AddScoped<IDocumentCloudServices, DocumentCloudServices>();
            services.AddScoped<IDocumentCloudCachedServices, DocumentCloudCachedServices>();
            services.AddScoped<IDocumentCollectionNameResolver, DocumentCollectionNameResolver>();
            services.AddScoped<IDocumentStorageClientProvider, DocumentStorageClientProvider>();
        }
    }
}
