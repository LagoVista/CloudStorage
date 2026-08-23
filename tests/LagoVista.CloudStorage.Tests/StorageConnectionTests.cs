using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests
{
    public class StorageConnectionTests
    {
        private static IConfiguration CreateConfiguration()
        {
            var values = new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints"] = "cassandra-0.cassandra.svc,cassandra-1.cassandra.svc,cassandra-0.cassandra.svc",
                ["CassandraStorage:UserName"] = "app",
                ["CassandraStorage:Password"] = "test-password",
                ["CassandraStorage:Keyspace"] = "nuviot",
                ["CassandraStorage:LocalDataCenter"] = "dc1",
                ["ScratchStorage:ConnectionString"] = "mongodb://localhost:27017",
                ["ScratchStorage:DatabaseName"] = "nuviot-scratch",
                ["ApplicationDataStorage:ConnectionString"] = "mongodb://localhost:27017",
                ["ApplicationDataStorage:DatabaseName"] = "nuviot-application"
            };

            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        [Test]
        public void CassandraSettings_ReadStandardApplicationConfiguration()
        {
            var settings = new CassandraStorageSettings(CreateConfiguration());
            Assert.That(settings.ContactPoints.Count, Is.EqualTo(2));
            Assert.That(settings.Port, Is.EqualTo(9042));
            Assert.That(settings.Keyspace, Is.EqualTo("nuviot"));
            Assert.That(settings.LocalDataCenter, Is.EqualTo("dc1"));
        }

        [Test]
        public void SemanticMongoSettings_RemainIndependent()
        {
            var configuration = CreateConfiguration();
            var scratch = new ScratchStorageSettings(configuration);
            var application = new ApplicationDataStorageSettings(configuration);

            Assert.That(scratch.ConnectionString, Is.EqualTo(application.ConnectionString));
            Assert.That(scratch.DatabaseName, Is.EqualTo("nuviot-scratch"));
            Assert.That(application.DatabaseName, Is.EqualTo("nuviot-application"));
        }

        [Test]
        public void Settings_ToString_DoesNotExposeSecrets()
        {
            var configuration = CreateConfiguration();
            Assert.That(new CassandraStorageSettings(configuration).ToString(), Does.Not.Contain("test-password"));
            Assert.That(new ScratchStorageSettings(configuration).ToString(), Does.Contain("<redacted>"));
            Assert.That(new ApplicationDataStorageSettings(configuration).ToString(), Does.Contain("<redacted>"));
        }

        [Test]
        public void StorageSettings_RegisterAsSingletonInterfaces()
        {
            var services = new ServiceCollection();
            services.AddSingleton(CreateConfiguration());
            services.AddCassandraStorageConnection();
            services.AddScratchStorageConnection();
            services.AddApplicationDataStorageConnection();

            using (var provider = services.BuildServiceProvider())
            {
                Assert.That(provider.GetRequiredService<ICassandraStorageSettings>(), Is.SameAs(provider.GetRequiredService<ICassandraStorageSettings>()));
                Assert.That(provider.GetRequiredService<IScratchStorageSettings>(), Is.SameAs(provider.GetRequiredService<IScratchStorageSettings>()));
                Assert.That(provider.GetRequiredService<IApplicationDataStorageSettings>(), Is.SameAs(provider.GetRequiredService<IApplicationDataStorageSettings>()));
                Assert.That(provider.GetRequiredService<IMongoStorageClientFactory>(), Is.SameAs(provider.GetRequiredService<IMongoStorageClientFactory>()));
            }
        }

        [Test]
        public void MongoClientFactory_ReusesClientForMatchingConnectionString()
        {
            var configuration = CreateConfiguration();
            var scratch = new ScratchStorageSettings(configuration);
            var application = new ApplicationDataStorageSettings(configuration);
            var factory = new MongoStorageClientFactory();

            Assert.That(factory.GetClient(scratch.ConnectionString), Is.SameAs(factory.GetClient(application.ConnectionString)));
        }
    }
}
