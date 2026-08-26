using LagoVista.CloudStorage.Storage.ConnectionSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace LagoVista.StorageProvider.Tests.Cassandra
{
    [TestClass]
    [TestCategory("CassandraInfrastructure")]
    public class CassandraStorageSettingsTests
    {
        [TestMethod]
        public void Constructor_ParsesAndNormalizesConfiguredValues()
        {
            var configuration = CreateConfiguration(new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints"] = " cassandra-1 ; cassandra-2,cassandra-1 ",
                ["CassandraStorage:UserName"] = "test-user",
                ["CassandraStorage:Password"] = "test-secret",
                ["CassandraStorage:Keyspace"] = "nuviot_test",
                ["CassandraStorage:Port"] = "19042",
                ["CassandraStorage:LocalDataCenter"] = " datacenter1 "
            });

            var settings = new CassandraStorageSettings(configuration);

            CollectionAssert.AreEqual(new[] { "cassandra-1", "cassandra-2" }, new List<string>(settings.ContactPoints));
            Assert.AreEqual("test-user", settings.UserName);
            Assert.AreEqual("test-secret", settings.Password);
            Assert.AreEqual("nuviot_test", settings.Keyspace);
            Assert.AreEqual(19042, settings.Port);
            Assert.AreEqual("datacenter1", settings.LocalDataCenter);
        }

        [TestMethod]
        public void Constructor_UsesPortAndDataCenterDefaults()
        {
            var configuration = CreateConfiguration(new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints"] = "127.0.0.1",
                ["CassandraStorage:UserName"] = "test-user",
                ["CassandraStorage:Password"] = "test-secret",
                ["CassandraStorage:Keyspace"] = "nuviot_test"
            });

            var settings = new CassandraStorageSettings(configuration);

            Assert.AreEqual(9042, settings.Port);
            Assert.IsNull(settings.LocalDataCenter);
        }

        [DataTestMethod]
        [DataRow("0")]
        [DataRow("65536")]
        [DataRow("not-a-port")]
        public void Constructor_RejectsInvalidPort(string port)
        {
            var configuration = CreateConfiguration(new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints"] = "127.0.0.1",
                ["CassandraStorage:UserName"] = "test-user",
                ["CassandraStorage:Password"] = "test-secret",
                ["CassandraStorage:Keyspace"] = "nuviot_test",
                ["CassandraStorage:Port"] = port
            });

            Assert.ThrowsExactly<InvalidOperationException>(() => new CassandraStorageSettings(configuration));
        }

        [TestMethod]
        public void Constructor_RejectsEmptyContactPointList()
        {
            var configuration = CreateConfiguration(new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints"] = " , ; ",
                ["CassandraStorage:UserName"] = "test-user",
                ["CassandraStorage:Password"] = "test-secret",
                ["CassandraStorage:Keyspace"] = "nuviot_test"
            });

            Assert.ThrowsExactly<InvalidOperationException>(() => new CassandraStorageSettings(configuration));
        }

        [TestMethod]
        public void Constructor_RejectsNullConfiguration()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new CassandraStorageSettings(null));
        }

        [TestMethod]
        public void ToString_RedactsPasswordAndShowsConnectionIdentity()
        {
            var configuration = CreateConfiguration(new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints"] = "cassandra-1,cassandra-2",
                ["CassandraStorage:UserName"] = "test-user",
                ["CassandraStorage:Password"] = "super-secret-value",
                ["CassandraStorage:Keyspace"] = "nuviot_test",
                ["CassandraStorage:Port"] = "19042"
            });

            var text = new CassandraStorageSettings(configuration).ToString();

            StringAssert.Contains(text, "cassandra-1,cassandra-2");
            StringAssert.Contains(text, "Port=19042");
            StringAssert.Contains(text, "Keyspace=nuviot_test");
            StringAssert.Contains(text, "UserName=test-user");
            StringAssert.Contains(text, "Password=<redacted>");
            Assert.IsFalse(text.Contains("super-secret-value", StringComparison.Ordinal));
        }

        private static IConfiguration CreateConfiguration(IDictionary<string, string> values)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }
    }
}
