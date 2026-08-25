using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.StorageProviders;
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
                ["MongoDocumentStorage:Hosts"] = "mongo-0.mongo.svc,mongo-1.mongo.svc,mongo-0.mongo.svc",
                ["MongoDocumentStorage:UserName"] = "mongo-app",
                ["MongoDocumentStorage:Password"] = "test:p@ssword",
                ["MongoDocumentStorage:AuthenticationDatabase"] = "admin",
                ["MongoDocumentStorage:ReplicaSet"] = "rs0",
                ["MongoDocumentStorage:UseTls"] = "true",
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
        public void MongoDocumentSettings_ReadStandardApplicationConfiguration()
        {
            var settings = new MongoDocumentStorageConnectionSettings(CreateConfiguration());
            Assert.That(settings.Hosts.Count, Is.EqualTo(2));
            Assert.That(settings.Port, Is.EqualTo(27017));
            Assert.That(settings.AuthenticationDatabase, Is.EqualTo("admin"));
            Assert.That(settings.ReplicaSet, Is.EqualTo("rs0"));
            Assert.That(settings.UseTls, Is.True);
        }

        [Test]
        public void MongoDocumentSettings_BuildConnectionStringFromComponents()
        {
            var settings = new MongoDocumentStorageConnectionSettings(CreateConfiguration());
            Assert.That(settings.BuildConnectionString(), Is.EqualTo("mongodb://mongo-app:test%3Ap%40ssword@mongo-0.mongo.svc:27017,mongo-1.mongo.svc:27017/?authSource=admin&replicaSet=rs0&tls=true"));
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
            Assert.That(new MongoDocumentStorageConnectionSettings(configuration).ToString(), Does.Not.Contain("test:p@ssword"));
            Assert.That(new ScratchStorageSettings(configuration).ToString(), Does.Contain("<redacted>"));
            Assert.That(new ApplicationDataStorageSettings(configuration).ToString(), Does.Contain("<redacted>"));
        }

        [Test]
        public void StorageSettingsAndMutableCapabilities_RegisterCorrectly()
        {
            var services = new ServiceCollection();
            services.AddSingleton(CreateConfiguration());
            services.AddCassandraStorageConnection();
            services.AddMongoDocumentStorageConnection();
            services.AddScratchStorageConnection();
            services.AddApplicationDataStorageConnection();

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                Assert.That(provider.GetRequiredService<ICassandraStorageSettings>(), Is.SameAs(provider.GetRequiredService<ICassandraStorageSettings>()));
                Assert.That(provider.GetRequiredService<IMongoDocumentStorageConnectionSettings>(), Is.SameAs(provider.GetRequiredService<IMongoDocumentStorageConnectionSettings>()));
                Assert.That(provider.GetRequiredService<IScratchStorageSettings>(), Is.SameAs(provider.GetRequiredService<IScratchStorageSettings>()));
                Assert.That(provider.GetRequiredService<IApplicationDataStorageSettings>(), Is.SameAs(provider.GetRequiredService<IApplicationDataStorageSettings>()));
                Assert.That(provider.GetRequiredService<IMongoStorageClientFactory>(), Is.SameAs(provider.GetRequiredService<IMongoStorageClientFactory>()));
                Assert.That(scope.ServiceProvider.GetRequiredService<IScratchStore>(), Is.TypeOf<MongoScratchStore>());
                Assert.That(scope.ServiceProvider.GetRequiredService<IApplicationDataStore>(), Is.TypeOf<MongoApplicationDataStore>());
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
