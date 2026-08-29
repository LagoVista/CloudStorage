using LagoVista.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class ActivityRecordStoreOptions<TRecord>
        where TRecord : IActivityRecord
    {
        internal ActivityRecordStoreOptions(StorageDefinition<TRecord> definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public StorageDefinition<TRecord> Definition { get; }
    }

    public sealed class OperationalDataStoreOptions<TRecord>
        where TRecord : class, IOperationalDataRecord
    {
        internal OperationalDataStoreOptions(Action<StorageDefinition<TRecord>> configure = null)
        {
            Definition = new StorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .PartitionBy(record => record.OrganizationId);

            configure?.Invoke(Definition);
        }

        public StorageDefinition<TRecord> Definition { get; }
    }

    public sealed class ScratchStoreOptions<TRecord>
        where TRecord : class, IScratchDataRecord
    {
        internal ScratchStoreOptions(Action<StorageDefinition<TRecord>> configure = null)
        {
            Definition = new StorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .PartitionBy(record => record.Organization.Id);

            configure?.Invoke(Definition);
        }

        public StorageDefinition<TRecord> Definition { get; }
    }

    public sealed class ApplicationDataStoreOptions<TRecord>
        where TRecord : class, IApplicationDataRecord
    {
        internal ApplicationDataStoreOptions(Action<StorageDefinition<TRecord>> configure = null)
        {
            Definition = new StorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .PartitionBy(record => record.Organization.Id);

            configure?.Invoke(Definition);
        }

        public StorageDefinition<TRecord> Definition { get; }
    }

    /// <summary>
    /// DI conventions for record-shaped storage capabilities. Per-record configuration
    /// declares additional query/index/retention behavior while identity and scope remain conventions.
    /// </summary>
    public static class StorageRegistrationExtensions
    {
        public static IServiceCollection AddActivityRecordStore<TRecord, TStore>(
            this IServiceCollection services,
            Action<StorageDefinition<TRecord>> configure = null)
            where TRecord : IActivityRecord
            where TStore : class, IActivityRecordStore<TRecord>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var definition = new StorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .TimeBy(record => record.CreationDate);

            configure?.Invoke(definition);

            services.AddSingleton(new ActivityRecordStoreOptions<TRecord>(definition));
            services.AddScoped<IActivityRecordStore<TRecord>, TStore>();
            return services;
        }

        public static IServiceCollection AddOperationalDataStore<TRecord, TStore>(
            this IServiceCollection services,
            Action<StorageDefinition<TRecord>> configure = null)
            where TRecord : class, IOperationalDataRecord
            where TStore : class, IOperationalDataStore<TRecord>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton(new OperationalDataStoreOptions<TRecord>(configure));
            services.AddScoped<IOperationalDataStore<TRecord>, TStore>();
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
            Action<StorageDefinition<TRecord>> configure)
            where TRecord : class, IScratchDataRecord
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            services.AddSingleton(new ScratchStoreOptions<TRecord>(configure));
            return services;
        }

        public static IServiceCollection ConfigureApplicationData<TRecord>(
            this IServiceCollection services,
            Action<StorageDefinition<TRecord>> configure)
            where TRecord : class, IApplicationDataRecord
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            services.AddSingleton(new ApplicationDataStoreOptions<TRecord>(configure));
            return services;
        }
    }
}
