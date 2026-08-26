using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cassandra
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Cassandra")]
    [TestCategory("CassandraInfrastructure")]
    public class CassandraSessionFactoryIntegrationTests
    {
        [TestMethod]
        public async Task GetSessionAsync_ConnectsToConfiguredKeyspaceAndReusesSession()
        {
            var settings = new TestCassandraStorageSettings();

            using var factory = new CassandraSessionFactory(settings);
            var first = await factory.GetSessionAsync();
            var second = await factory.GetSessionAsync();

            Assert.IsNotNull(first);
            Assert.AreSame(first, second);
            Assert.AreEqual(settings.Keyspace, first.Keyspace);

            var rows = await first.ExecuteAsync(new global::Cassandra.SimpleStatement("SELECT keyspace_name FROM system_schema.keyspaces WHERE keyspace_name = ?", settings.Keyspace));
            Assert.IsNotNull(rows.FirstOrDefault());
        }

        private sealed class TestCassandraStorageSettings : ICassandraStorageSettings
        {
            public IReadOnlyList<string> ContactPoints { get; } = new[] { "127.0.0.1" };
            public string UserName => "cassandra";
            public string Password => "cassandra";
            public string Keyspace => "nuviot_storage_tests";
            public int Port => 19042;
            public string LocalDataCenter => "datacenter1";
        }
    }
}
