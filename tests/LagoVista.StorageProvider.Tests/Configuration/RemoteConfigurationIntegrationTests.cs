using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Configuration
{
    [TestClass]
    public class RemoteConfigurationIntegrationTests
    {
        private const string AppKey = "web";
        private const string EnvironmentKey = "live";
        private const string DefaultConfigurationServiceBaseUrl = "https://config.nuviot.com";
        private const string LiveTokenEnvironmentVariable = "CFG_SRVR_LIVE";
        private const string ConfigurationServiceBaseUrlEnvironmentVariable = "CFG_SRVR_URL";

        [TestMethod]
        [TestCategory("Integration")]
        public async Task WebLiveRemoteConfigurationResolvesCassandraSettingsThroughNormalCloudStorageDI()
        {
            var token = Environment.GetEnvironmentVariable(LiveTokenEnvironmentVariable);
            if (String.IsNullOrWhiteSpace(token))
            {
                Assert.Inconclusive($"Set {LiveTokenEnvironmentVariable} to run the live remote configuration integration test.");
            }

            var baseUrl = Environment.GetEnvironmentVariable(ConfigurationServiceBaseUrlEnvironmentVariable);
            if (String.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = DefaultConfigurationServiceBaseUrl;
            }

            var services = new ServiceCollection();
            services.AddRemoteConfigurationClient();

            IConfigurationRoot configuration;
            using (var bootstrapProvider = services.BuildServiceProvider())
            {
                var client = bootstrapProvider.GetRequiredService<IRemoteConfigurationClient>();
                configuration = await client.LoadAsync(
                    new RemoteConfigurationSettings
                    {
                        ConfigurationServiceBaseUrl = baseUrl,
                        AuthorizationToken = token
                    },
                    AppKey,
                    EnvironmentKey);
            }

            Assert.IsNotNull(configuration);
            Assert.IsFalse(String.IsNullOrWhiteSpace(configuration["CassandraStorage:ContactPoints"]), "Remote configuration did not contain CassandraStorage:ContactPoints.");
            Assert.IsFalse(String.IsNullOrWhiteSpace(configuration["CassandraStorage:UserName"]), "Remote configuration did not contain CassandraStorage:UserName.");
            Assert.IsFalse(String.IsNullOrWhiteSpace(configuration["CassandraStorage:Keyspace"]), "Remote configuration did not contain CassandraStorage:Keyspace.");

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IConfigurationRoot>(configuration);
            LagoVista.CloudStorage.Startup.ConfigureServices(services);

            using var provider = services.BuildServiceProvider();
            var settings = provider.GetRequiredService<ICassandraStorageSettings>();

            Assert.IsInstanceOfType<CassandraStorageSettings>(settings);
            Assert.IsTrue(settings.ContactPoints.Any(), "CloudStorage DI resolved Cassandra settings without any contact points.");
            Assert.IsFalse(String.IsNullOrWhiteSpace(settings.UserName), "CloudStorage DI resolved Cassandra settings without a user name.");
            Assert.IsFalse(String.IsNullOrWhiteSpace(settings.Password), "CloudStorage DI resolved Cassandra settings without a password.");
            Assert.IsFalse(String.IsNullOrWhiteSpace(settings.Keyspace), "CloudStorage DI resolved Cassandra settings without a keyspace.");
            Assert.IsTrue(settings.Port > 0, "CloudStorage DI resolved an invalid Cassandra port.");
        }
    }
}
