using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICacheProvider, CacheProvider>();
        }
    }
}
