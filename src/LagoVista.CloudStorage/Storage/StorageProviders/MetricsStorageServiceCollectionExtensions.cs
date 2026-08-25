using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public static class MetricsStorageServiceCollectionExtensions
    {
        public static IServiceCollection AddMetricsStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IMetricsStorageSettings, MetricsStorageSettings>();
            return services;
        }

        public static IServiceCollection AddMetricsStore<TStore>(this IServiceCollection services)
            where TStore : class, IMetricsStore
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddMetricsStorageConnection();
            services.AddScoped<IMetricsStore, TStore>();
            return services;
        }
    }
}
