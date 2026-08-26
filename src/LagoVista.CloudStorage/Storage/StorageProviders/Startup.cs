using LagoVista.CloudStorage.StorageProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Storage.StorageProviders
{
    public static class Startup
    {

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICassandraSessionFactory, CassandraSessionFactory>();
            services.AddSingleton<IMongoStorageClientFactory, MongoStorageClientFactory>();
            services.AddSingleton<IApplicationDataStore, MongoApplicationDataStore>();
            services.AddSingleton<IScratchStore, MongoScratchStore>();



            services.AddSingleton<IMongoStorageClientFactory, MongoStorageClientFactory>();

        }
    }
}
