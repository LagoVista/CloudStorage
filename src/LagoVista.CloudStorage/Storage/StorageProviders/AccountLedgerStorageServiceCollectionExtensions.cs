using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public static class AccountLedgerStorageServiceCollectionExtensions
    {
        public static IServiceCollection AddAccountLedgerStorageConnection(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IAccountLedgerStorageSettings, AccountLedgerStorageSettings>();
            return services;
        }

        public static IServiceCollection AddAccountLedgerStore<TRecord, TStore>(this IServiceCollection services)
            where TRecord : IAccountLedgerRecord
            where TStore : class, IAccountLedgerStore<TRecord>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddAccountLedgerStorageConnection();
            services.AddScoped<IAccountLedgerStore<TRecord>, TStore>();
            return services;
        }
    }
}
