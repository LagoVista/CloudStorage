using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cassandra
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICassandraSessionFactory, CassandraSessionFactory>();
            services.AddScoped<LagoVista.CloudStorage.Storage.IOperationJournalStore, CassandraOperationJournalStore>();
            services.AddSingleton<LagoVista.CloudStorage.Interfaces.ICassandraAdminRepo, CassandraAdminRepo>();
        }
    }
}
