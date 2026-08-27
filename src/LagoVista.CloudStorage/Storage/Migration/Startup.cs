using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Storage.Migration
{
    internal static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDocumentMigrationService, DocumentMigrationService>();
        }
    }
}
