using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public static class StorageConnectionServiceCollectionExtensions
    {
        public static IServiceCollection AddCassandraStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<ICassandraStorageSettings, CassandraStorageSettings>();
            return services;
        }

        public static IServiceCollection AddScratchStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IScratchStorageSettings, ScratchStorageSettings>();
            services.TryAddSingleton<IMongoStorageClientFactory, MongoStorageClientFactory>();
            return services;
        }

        public static IServiceCollection AddApplicationDataStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IApplicationDataStorageSettings, ApplicationDataStorageSettings>();
            services.TryAddSingleton<IMongoStorageClientFactory, MongoStorageClientFactory>();
            return services;
        }
    }
}
