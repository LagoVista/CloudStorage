using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Configuration
{
    [TestClass]
    public class RemoteConfigurationIntegrationTests
    {
        private const string DefaultAppKey = "web";
        private const string DefaultEnvironmentKey = "live";
        private const string DefaultConfigurationServiceBaseUrl = "https://config.nuviot.com";
        private const string AppKeyEnvironmentVariable = "CFG_APP_KEY";
        private const string EnvironmentKeyEnvironmentVariable = "CFG_ENVIRONMENT_KEY";
        private const string ConfigurationServiceBaseUrlEnvironmentVariable = "CFG_SRVR_URL";

        [TestMethod]
        [TestCategory("Integration")]
        public async Task RemoteConfigurationResolvesCassandraSettingsThroughNormalCloudStorageDI()
        {
            var appKey = ReadOptionalEnvironmentVariable(AppKeyEnvironmentVariable) ?? DefaultAppKey;
            var environmentKey = ReadOptionalEnvironmentVariable(EnvironmentKeyEnvironmentVariable) ?? DefaultEnvironmentKey;
            var tokenEnvironmentVariable = BuildTokenEnvironmentVariableName(appKey, environmentKey);
            var token = Environment.GetEnvironmentVariable(tokenEnvironmentVariable);

            if (String.IsNullOrWhiteSpace(token))
            {
                Assert.Inconclusive($"Set {tokenEnvironmentVariable} to run the remote configuration integration test for app '{appKey}' and environment '{environmentKey}'.");
            }

            var baseUrl = ReadOptionalEnvironmentVariable(ConfigurationServiceBaseUrlEnvironmentVariable)
                ?? DefaultConfigurationServiceBaseUrl;

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
                    appKey,
                    environmentKey);
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

        private static string BuildTokenEnvironmentVariableName(string appKey, string environmentKey)
        {
            return $"CFG_{ToEnvironmentVariableSegment(appKey)}_{ToEnvironmentVariableSegment(environmentKey)}_TOKEN";
        }

        private static string ToEnvironmentVariableSegment(string value)
        {
            var result = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                result.Append(Char.IsLetterOrDigit(character) ? Char.ToUpperInvariant(character) : '_');
            }

            return result.ToString();
        }

        private static string ReadOptionalEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
