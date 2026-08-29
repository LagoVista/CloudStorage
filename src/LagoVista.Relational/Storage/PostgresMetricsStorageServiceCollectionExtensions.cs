using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LagoVista.Relational.Storage
{
    public static class PostgresMetricsStorageServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgresMetricsStore(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddMetricsStore<PostgresMetricsStore>();
            return services;
        }
    }
}
