using LagoVista.Core.Interfaces;
using LagoVista.Core.Interfaces.AutoMapper;
using LagoVista.Relational.Converters;

namespace LagoVista.Relational
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMapValueConverter, EntityHeaderDtoConverter>();
        }
    }
}
