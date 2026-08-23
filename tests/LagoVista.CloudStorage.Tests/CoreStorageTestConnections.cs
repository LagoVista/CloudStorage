using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests
{
    /// <summary>
    /// Fixed connection presets for disposable local Docker integration-test services.
    /// These values intentionally match the docker-compose test harness and are not
    /// used for development, staging, or production connections.
    /// </summary>
    internal static class CoreStorageTestConnections
    {
        public static ICassandraStorageSettings Cassandra => CreateCassandraSettings();

        private static CassandraStorageSettings CreateCassandraSettings()
        {
            var values = new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints:0"] = "localhost",
                ["CassandraStorage:Port"] = "19042",
                ["CassandraStorage:UserName"] = "cassandra",
                ["CassandraStorage:Password"] = "cassandra",
                ["CassandraStorage:Keyspace"] = "nuviot_cloudstorage_tests",
                ["CassandraStorage:LocalDataCenter"] = "datacenter1"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            return new CassandraStorageSettings(configuration);
        }
    }
}
