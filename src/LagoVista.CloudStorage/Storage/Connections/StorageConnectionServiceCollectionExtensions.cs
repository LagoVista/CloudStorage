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
            services.TryAddSingleton<ICassandraSessionFactory, CassandraSessionFactory>();
            return services;
        }

        public static IServiceCollection AddMongoDocumentStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IMongoDocumentStorageConnectionSettings, MongoDocumentStorageConnectionSettings>();
            services.TryAddSingleton<IMongoStorageClientFactory, MongoStorageClientFactory>();
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
