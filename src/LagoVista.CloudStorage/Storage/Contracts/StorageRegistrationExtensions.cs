using System;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class AppendHistoryStoreOptions<TEntity>
    {
        internal AppendHistoryStoreOptions(FlatStorageDefinition<TEntity> definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (String.IsNullOrWhiteSpace(definition.TimeField))
            {
                throw new InvalidOperationException($"Append history storage for {typeof(TEntity).Name} requires a canonical TimeBy(...) field.");
            }
        }

        public FlatStorageDefinition<TEntity> Definition { get; }
    }

    public sealed class ScratchStoreOptions<TEntity>
    {
        internal ScratchStoreOptions(FlatStorageDefinition<TEntity> definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (String.IsNullOrWhiteSpace(definition.KeyField))
            {
                throw new InvalidOperationException($"Scratch storage for {typeof(TEntity).Name} requires a KeyBy(...) field.");
            }
        }

        public FlatStorageDefinition<TEntity> Definition { get; }
    }

    public sealed class ApplicationDataStoreOptions<TEntity>
    {
        internal ApplicationDataStoreOptions(FlatStorageDefinition<TEntity> definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (String.IsNullOrWhiteSpace(definition.KeyField))
            {
                throw new InvalidOperationException($"Application data storage for {typeof(TEntity).Name} requires a KeyBy(...) field.");
            }
        }

        public FlatStorageDefinition<TEntity> Definition { get; }
    }

    /// <summary>
    /// DI conventions for storage capabilities. Repositories depend on capability
    /// interfaces only; provider choice lives in composition-root registration.
    /// </summary>
    public static class StorageRegistrationExtensions
    {
        public static IServiceCollection AddAppendHistoryStore<TEntity, TStore>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TEntity>> configure)
            where TStore : class, IAppendHistoryStore<TEntity>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var definition = BuildDefinition(configure);
            services.AddSingleton(new AppendHistoryStoreOptions<TEntity>(definition));
            services.AddScoped<IAppendHistoryStore<TEntity>, TStore>();
            return services;
        }

        public static IServiceCollection AddScratchStore<TEntity, TStore>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TEntity>> configure)
            where TStore : class, IScratchStore<TEntity>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var definition = BuildDefinition(configure);
            services.AddSingleton(new ScratchStoreOptions<TEntity>(definition));
            services.AddScoped<IScratchStore<TEntity>, TStore>();
            return services;
        }

        public static IServiceCollection AddApplicationDataStore<TEntity, TStore>(
            this IServiceCollection services,
            Action<FlatStorageDefinition<TEntity>> configure)
            where TStore : class, IApplicationDataStore<TEntity>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var definition = BuildDefinition(configure);
            services.AddSingleton(new ApplicationDataStoreOptions<TEntity>(definition));
            services.AddScoped<IApplicationDataStore<TEntity>, TStore>();
            return services;
        }

        private static FlatStorageDefinition<TEntity> BuildDefinition<TEntity>(Action<FlatStorageDefinition<TEntity>> configure)
        {
            var definition = new FlatStorageDefinition<TEntity>();
            configure(definition);
            return definition;
        }
    }
}
