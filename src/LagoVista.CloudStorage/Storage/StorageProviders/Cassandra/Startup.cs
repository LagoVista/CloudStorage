using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cassandra
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICassandraSessionFactory, CassandraSessionFactory>();
        }
    }
}
