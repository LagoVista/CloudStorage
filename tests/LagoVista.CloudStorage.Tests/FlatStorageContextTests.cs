using LagoVista.CloudStorage.Storage;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class FlatStorageContextTests
    {
        [Test]
        public void AzureFactory_CreatesAzureContext()
        {
            var context = FlatStorageContext.AzureTableStorage("account", "key");

            Assert.That(context.Provider, Is.EqualTo(FlatStorageProvider.AzureTableStorage));
            var connection = context.GetConnection<AzureTableStorageConnectionSettings>();
            Assert.That(connection.AccountId, Is.EqualTo("account"));
            Assert.That(connection.AccountKey, Is.EqualTo("key"));
        }

        [Test]
        public void CassandraContext_CarriesClusterConnectionDetails()
        {
            var settings = new CassandraConnectionSettings(
                new[] { "cassandra-0.cassandra.svc", "cassandra-1.cassandra.svc" },
                "user",
                "password",
                "nuviot",
                localDataCenter: "dc1");

            var context = new FlatStorageContext(FlatStorageProvider.Cassandra, settings);

            Assert.That(context.Provider, Is.EqualTo(FlatStorageProvider.Cassandra));
            Assert.That(settings.Port, Is.EqualTo(9042));
            Assert.That(settings.Keyspace, Is.EqualTo("nuviot"));
            Assert.That(settings.LocalDataCenter, Is.EqualTo("dc1"));
            Assert.That(settings.ContactPoints.Count, Is.EqualTo(2));
        }

        [Test]
        public void Context_WithMismatchedConnectionType_FailsFast()
        {
            Assert.Throws<ArgumentException>(() =>
                new FlatStorageContext(
                    FlatStorageProvider.Cassandra,
                    new AzureTableStorageConnectionSettings("account", "key")));
        }

        [Test]
        public void GetConnection_WithWrongType_FailsFast()
        {
            var context = FlatStorageContext.AzureTableStorage("account", "key");

            Assert.Throws<InvalidOperationException>(() =>
                context.GetConnection<CassandraConnectionSettings>());
        }
    }
}
