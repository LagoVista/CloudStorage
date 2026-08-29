using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    internal static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDefaultConnectionSettings, DefaultConnectionSettings>();
            services.AddSingleton<ICassandraStorageSettings, CassandraStorageSettings>();
            services.AddSingleton<IDocumentStorageProviderSettings, DocumentStorageProviderSettings>();
            services.AddSingleton<IMongoDocumentStorageConnectionSettings, MongoDocumentStorageConnectionSettings>();
            services.AddSingleton<IScratchStorageSettings, ScratchStorageSettings>();
            services.AddSingleton<IApplicationDataStorageSettings, ApplicationDataStorageSettings>();
            services.AddSingleton<ICosmosConnectionSettings, CosmosConnectionSettings>();
            services.AddSingleton<ISyncConnectionSettings, SyncConnections>();
            services.AddSingleton<IMetricsStorageSettings, MetricsStorageSettings>();
            services.AddSingleton<IAccountLedgerStorageSettings, AccountLedgerStorageSettings>();
            services.AddSingleton<IPostgresConnectionSettings, PostgresConnectionSettings>();
            services.AddSingleton<IS3ObjectStorageConnectionSettings, S3ObjectStorageConnectionSettings>();
        }
    }
}
