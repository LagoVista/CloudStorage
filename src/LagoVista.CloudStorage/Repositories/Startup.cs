using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Repositories
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISyncRepository, SyncRepository>();
            services.AddTransient<IEntityDetailResponseFactory, EntityDetailResponseFactory>();
            services.AddTransient<IEntityPreparationCandidateRepository, EntityPreparationCandidateRepository>();
            services.AddTransient<IEntityUtilsRepository, EntityUtilsRepository>();
        }
    }
}
