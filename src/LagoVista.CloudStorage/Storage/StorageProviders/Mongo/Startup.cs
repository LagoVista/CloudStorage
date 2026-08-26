using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.StorageProviders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IMongoStorageClientFactory, MongoStorageClientFactory>();
            services.AddSingleton<IApplicationDataStore, MongoApplicationDataStore>();
            services.AddSingleton<IScratchStore, MongoScratchStore>();
            services.AddSingleton<IMongoDocumentStorageClient, MongoDocumentStorageClient>();
        }
    }
}
