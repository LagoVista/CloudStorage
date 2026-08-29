using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LagoVista.Relational.Storage
{
    public static class PostgresAccountLedgerStorageServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgresAccountLedgerStore<TRecord>(this IServiceCollection services) where TRecord : class, IAccountLedgerRecord
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddAccountLedgerStore<TRecord, PostgresAccountLedgerStore<TRecord>>();
            return services;
        }
    }
}
