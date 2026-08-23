using System;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class ActivityRecordStoreOptions<TRecord>
        where TRecord : IActivityRecord
    {
        internal ActivityRecordStoreOptions(FlatStorageDefinition<TRecord> definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public FlatStorageDefinition<TRecord> Definition { get; }
    }

    public sealed class ScratchStoreOptions<TRecord>
        where TRecord : IScratchDataRecord
    {
        internal ScratchStoreOptions(FlatStorageDefinition<TRecord> definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (String.IsNullOrWhiteSpace(definition.KeyField))
            {
                throw new InvalidOperationException($"Scratch storage for {typeof(TRecord).Name} requires a KeyBy(...) field.");
            }
        }

        public FlatStorageDefinition<TRecord> Definition { get; }
    }

    public sealed class ApplicationDataStoreOptions<TRecord>
        where TRecord : IApplicationDataRecord
    {
        internal ApplicationDataStoreOptions(FlatStorageDefinition<TRecord> definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (String.IsNullOrWhiteSpace(definition.KeyField))
            {
                throw new InvalidOperationException($"Application data storage for {typeof(TRecord).Name} requires a KeyBy(...) field.");
            }
        }

        public FlatStorageDefinition<TRecord> Definition { get; }
    }

    /// <summary>
    /// DI conventions for storage capabilities. Repositories depend on capability
    /// interfaces only; provider choice lives in composition-root registration.
    /// </summary>
    public static class StorageRegistrationExtensions
    {
        public static IServiceCollection AddActivityRecordStore<TRecord, TStore>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TRecord>> configure = null)
            where TRecord : IActivityRecord
            where TStore : class, IActivityRecordStore<TRecord>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var definition = new FlatStorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .TimeBy(record => record.CreationDate);

            configure?.Invoke(definition);

            services.AddSingleton(new ActivityRecordStoreOptions<TRecord>(definition));
            services.AddScoped<IActivityRecordStore<TRecord>, TStore>();
            return services;
        }

        public static IServiceCollection AddScratchStore<TRecord, TStore>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TRecord>> configure)
            where TRecord : IScratchDataRecord
            where TStore : class, IScratchStore<TRecord>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var definition = BuildDefinition(configure);
            services.AddSingleton(new ScratchStoreOptions<TRecord>(definition));
            services.AddScoped<IScratchStore<TRecord>, TStore>();
            return services;
        }

        public static IServiceCollection AddApplicationDataStore<TRecord, TStore>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TRecord>> configure)
            where TRecord : IApplicationDataRecord
            where TStore : class, IApplicationDataStore<TRecord>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var definition = BuildDefinition(configure);
            services.AddSingleton(new ApplicationDataStoreOptions<TRecord>(definition));
            services.AddScoped<IApplicationDataStore<TRecord>, TStore>();
            return services;
        }

        public static IServiceCollection AddAccountLedgerStore<TRecord, TStore>(this IServiceCollection services)
            where TRecord : IAccountLedgerRecord
            where TStore : class, IAccountLedgerStore<TRecord>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.AddScoped<IAccountLedgerStore<TRecord>, TStore>();
            return services;
        }

        private static FlatStorageDefinition<TRecord> BuildDefinition<TRecord>(Action<FlatStorageDefinition<TRecord>> configure)
        {
            var definition = new FlatStorageDefinition<TRecord>();
            configure(definition);
            return definition;
        }
    }
}
