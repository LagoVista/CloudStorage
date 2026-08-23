using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public static class StorageConnectionServiceCollectionExtensions
    {
        public const string CassandraSectionName = "Storage:Cassandra";
        public const string MongoSectionName = "Storage:Mongo";

        public static IServiceCollection AddCassandraStorageConnection(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = CassandraSectionName)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (String.IsNullOrWhiteSpace(sectionName)) throw new ArgumentNullException(nameof(sectionName));

            var settings = CassandraStorageSettings.FromConfiguration(configuration.GetSection(sectionName));
            services.TryAddSingleton(settings);
            return services;
        }

        public static IServiceCollection AddCassandraStorageConnection(
            this IServiceCollection services,
            CassandraStorageSettings settings)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            services.TryAddSingleton(settings);
            return services;
        }

        public static IServiceCollection AddMongoStorageConnection(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = MongoSectionName)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (String.IsNullOrWhiteSpace(sectionName)) throw new ArgumentNullException(nameof(sectionName));

            var settings = MongoStorageSettings.FromConfiguration(configuration.GetSection(sectionName));
            return services.AddMongoStorageConnection(settings);
        }

        public static IServiceCollection AddMongoStorageConnection(
            this IServiceCollection services,
            MongoStorageSettings settings)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            services.TryAddSingleton(settings);
            services.TryAddSingleton<IMongoStorageClientProvider, MongoStorageClientProvider>();
            return services;
        }
    }
}
