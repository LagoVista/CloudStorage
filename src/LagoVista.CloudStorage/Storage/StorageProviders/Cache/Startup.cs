using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cache
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICacheProvider, CacheProvider>();
            services.AddTransient<IEntityListItemCache, EntityListItemCache>();
        }
    }
}
