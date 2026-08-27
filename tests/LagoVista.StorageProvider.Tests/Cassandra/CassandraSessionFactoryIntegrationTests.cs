using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [TestMethod]
        public async Task GetSessionAsync_MissingKeyspaceFails()
        {
            var settings = new TestCassandraStorageSettings($"missing_{Guid.NewGuid():N}");

            using var factory = new CassandraSessionFactory(settings);
            await Assert.ThrowsExactlyAsync<global::Cassandra.InvalidQueryException>(() => factory.GetSessionAsync());
        }

        private sealed class TestCassandraStorageSettings : ICassandraStorageSettings
        {
            public TestCassandraStorageSettings(string keyspace = "nuviot_storage_tests")
            {
                Keyspace = keyspace;
            }

            public IReadOnlyList<string> ContactPoints { get; } = new[] { "127.0.0.1" };
            public string UserName => "cassandra";
            public string Password => "cassandra";
            public string Keyspace { get; } = String.Empty;
            public int Port => 19042;
            public string LocalDataCenter => "datacenter1";
        }
    }
}
