using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.StorageProviders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Storage.StorageProviders.CosmosDB
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICosmosClientProvider, CosmosClientProvider>();
            services.AddTransient<ICosmosDocumentStorageClient, CosmosDocumentStorageClient>();
        }
    }
}
