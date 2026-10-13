using LagoVista.Core.PlatformSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.CloudStorage
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            // F216 acceptance: the compiler must resolve this new API from the
            // published LagoVista.Core NuGet dependency, not a local project.
            _ = LagoVista.Core.Startup.F216NuGetAcceptanceMarker();

            // F218: these new symbols exist only in the two newly published NuGets.
            _ = LagoVista.Core.Startup.F218CorePackageMarker();
            _ = LagoVista.IoT.Logging.Startup.F218LoggingPackageMarker();

            Storage.ConnectionSettings.Startup.ConfigureServices(services);
            Storage.StorageProviders.Startup.ConfigureServices(services);
            Storage.Migration.Startup.ConfigureServices(services);
            Repositories.Startup.ConfigureServices(services);
            Managers.Startup.ConfigureServices(services);
            Utils.Startup.ConfigureServices(services);
            Diagnostics.Startup.ConfigureServices(services);

            LagoVista.Core.AutoMapper.Startup.ConfigureServices(services);
        }
    }
}

namespace LagoVista.DependencyInjection
{
    public static class CloudStorageModule
    {
        public static void AddCloudStorageModule(this IServiceCollection services, IConfigurationRoot configurationRoot, ILogger logger)
        {
            LagoVista.CloudStorage.Startup.ConfigureServices(services);
        }
    }
}
