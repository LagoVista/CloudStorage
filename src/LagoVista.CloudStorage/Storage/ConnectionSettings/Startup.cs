using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    internal static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICassandraStorageSettings, CassandraStorageSettings>();
            services.AddSingleton<IMongoDocumentStorageConnectionSettings, MongoDocumentStorageConnectionSettings>();
            services.AddSingleton<IScratchStorageSettings, ScratchStorageSettings>();
            services.AddSingleton<IApplicationDataStorageSettings, ApplicationDataStorageSettings>();
        }
    }
}
