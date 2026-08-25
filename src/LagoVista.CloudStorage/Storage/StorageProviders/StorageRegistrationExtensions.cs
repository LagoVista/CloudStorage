using LagoVista.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

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
        where TRecord : class, IScratchDataRecord
    {
        internal ScratchStoreOptions(Action<FlatStorageDefinition<TRecord>> configure = null)
        {
            Definition = new FlatStorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .PartitionBy(record => record.Organization.Id);

            configure?.Invoke(Definition);
        }

        public FlatStorageDefinition<TRecord> Definition { get; }
    }

    public sealed class ApplicationDataStoreOptions<TRecord>
        where TRecord : class, IApplicationDataRecord
    {
        internal ApplicationDataStoreOptions(Action<FlatStorageDefinition<TRecord>> configure = null)
        {
            Definition = new FlatStorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .PartitionBy(record => record.Organization.Id);

            configure?.Invoke(Definition);
        }

        public FlatStorageDefinition<TRecord> Definition { get; }
    }

    /// <summary>
    /// DI conventions for record-shaped storage capabilities. Mutable application and
    /// scratch stores are registered once. Per-record configuration is optional and
    /// only declares additional query/index/retention behavior; identity and scope are conventions.
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

        public static IServiceCollection AddScratchStore<TStore>(this IServiceCollection services)
            where TStore : class, IScratchStore
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddScoped<IScratchStore, TStore>();
            return services;
        }

        public static IServiceCollection AddApplicationDataStore<TStore>(this IServiceCollection services)
            where TStore : class, IApplicationDataStore
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddScoped<IApplicationDataStore, TStore>();
            return services;
        }

        public static IServiceCollection ConfigureScratchData<TRecord>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TRecord>> configure)
            where TRecord : class, IScratchDataRecord
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            services.AddSingleton(new ScratchStoreOptions<TRecord>(configure));
            return services;
        }

        public static IServiceCollection ConfigureApplicationData<TRecord>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TRecord>> configure)
            where TRecord : class, IApplicationDataRecord
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            services.AddSingleton(new ApplicationDataStoreOptions<TRecord>(configure));
            return services;
        }
    }
}
