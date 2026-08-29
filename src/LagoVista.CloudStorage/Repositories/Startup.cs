using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage.Repositories
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ISyncRepository, SyncRepository>();
            services.AddScoped<IEntityDetailResponseFactory, EntityDetailResponseFactory>();
            services.AddScoped<IEntityPreparationCandidateRepository, EntityPreparationCandidateRepository>();
            services.AddScoped<IEntityUtilsRepository, EntityUtilsRepository>();
            services.AddScoped<IEntityListItemRepoFactory, EntityListItemRepoFactory>();
        }
    }
}
