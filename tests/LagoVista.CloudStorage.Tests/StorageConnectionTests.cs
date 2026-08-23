using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class StorageConnectionTests
    {
        [Test]
        public void CassandraSettings_NormalizeConnectionDetails()
        {
            var settings = new CassandraStorageSettings(
                new[] { " cassandra-0.cassandra.svc ", "cassandra-1.cassandra.svc", "cassandra-0.cassandra.svc" },
                "app",
                "secret",
                "nuviot",
                localDataCenter: " dc1 ");

            Assert.That(settings.ContactPoints.Count, Is.EqualTo(2));
            Assert.That(settings.ContactPoints[0], Is.EqualTo("cassandra-0.cassandra.svc"));
            Assert.That(settings.Port, Is.EqualTo(9042));
            Assert.That(settings.Keyspace, Is.EqualTo("nuviot"));
            Assert.That(settings.LocalDataCenter, Is.EqualTo("dc1"));
        }

        [Test]
        public void CassandraSettings_ToString_DoesNotExposePassword()
        {
            var settings = new CassandraStorageSettings(
                new[] { "cassandra.svc" },
                "app",
                "super-secret-value",
                "nuviot");

            Assert.That(settings.ToString(), Does.Not.Contain("super-secret-value"));
            Assert.That(settings.ToString(), Does.Contain("<redacted>"));
        }

        [Test]
        public void MongoSettings_ToString_DoesNotExposeConnectionString()
        {
            var settings = new MongoStorageSettings(
                "mongodb://user:super-secret-value@mongodb.svc",
                "nuviot");

            Assert.That(settings.ToString(), Does.Not.Contain("super-secret-value"));
            Assert.That(settings.ToString(), Does.Not.Contain("mongodb://"));
            Assert.That(settings.ToString(), Does.Contain("<redacted>"));
        }

        [Test]
        public void MongoStorageConnection_UsesSingletonClientProvider()
        {
            var services = new ServiceCollection();
            services.AddMongoStorageConnection(new MongoStorageSettings("mongodb://localhost:27017", "nuviot"));

            using (var provider = services.BuildServiceProvider())
            {
                var first = provider.GetRequiredService<IMongoStorageClientProvider>();
                var second = provider.GetRequiredService<IMongoStorageClientProvider>();

                Assert.That(first, Is.SameAs(second));
                Assert.That(first.Settings.DefaultDatabaseName, Is.EqualTo("nuviot"));
                Assert.That(first.Client, Is.SameAs(second.Client));
            }
        }

        [Test]
        public void CassandraStorageConnection_UsesSingletonSettings()
        {
            var services = new ServiceCollection();
            var settings = new CassandraStorageSettings(
                new[] { "cassandra.svc" },
                "app",
                "secret",
                "nuviot");

            services.AddCassandraStorageConnection(settings);

            using (var provider = services.BuildServiceProvider())
            {
                Assert.That(provider.GetRequiredService<CassandraStorageSettings>(), Is.SameAs(settings));
            }
        }

        [Test]
        public void MongoDatabase_RequiresConfiguredOrExplicitName()
        {
            var clientProvider = new MongoStorageClientProvider(new MongoStorageSettings("mongodb://localhost:27017"));

            Assert.Throws<InvalidOperationException>(() => clientProvider.GetDatabase());
        }

        [Test]
        public void InvalidSettings_FailImmediately()
        {
            Assert.Throws<ArgumentException>(() =>
                new CassandraStorageSettings(Array.Empty<string>(), "app", "secret", "nuviot"));

            Assert.Throws<ArgumentNullException>(() =>
                new MongoStorageSettings(null));
        }
    }
}
