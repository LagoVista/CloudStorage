using LagoVista.CloudStorage.Diagnostics;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public static class StorageConnectionServiceCollectionExtensions
    {
        public static IServiceCollection AddCassandraStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddEnumerable(ServiceDescriptor.Transient<IPlatformSmokeTest, CassandraPlatformSmokeTest>());
            return services;
        }

        public static IServiceCollection AddMongoDocumentStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return services;
        }

        public static IServiceCollection AddScratchStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IScratchStore, MongoScratchStore>();
            return services;
        }

        public static IServiceCollection AddApplicationDataStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IMongoStorageClientFactory, MongoStorageClientFactory>();
            return services;
        }
    }
}
