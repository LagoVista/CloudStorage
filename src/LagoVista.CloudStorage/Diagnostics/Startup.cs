using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Diagnostics
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IPlatformSmokeTest, CassandraPlatformSmokeTest>();
            services.AddTransient<IPlatformSmokeTest, MongoDocumentStorageSmokeTest>();
            services.AddTransient<IPlatformSmokeTest, ScratchStorageSmokeTest>();
            services.AddTransient<IPlatformSmokeTest, ApplicationStorageSmokeTest>();
            services.AddTransient<IPlatformSmokeTest, CosmosDocumentStorageSmokeTest>();
            services.AddTransient<IPlatformSmokeTest, RedisPlatformSmokeTest>();
        }
    }
}
