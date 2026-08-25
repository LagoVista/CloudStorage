using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    /// <summary>
    /// Convenience settings for tools that explicitly target the shared Development or Production
    /// Cassandra environments. Runtime applications should normally use CassandraStorageSettings
    /// through IConfiguration and the CassandraStorage section.
    /// </summary>
    public static class CassandraStorageEnvironmentSettings
    {
        public static ICassandraStorageSettings Development => Create("DEV");

        public static ICassandraStorageSettings Production => Create("PROD");

        private static ICassandraStorageSettings Create(string prefix)
        {
            var values = new Dictionary<string, string>
            {
                [$"{CassandraStorageSettings.SectionName}:ContactPoints"] = Environment.GetEnvironmentVariable($"{prefix}_CASSANDRA_CONTACT_POINTS"),
                [$"{CassandraStorageSettings.SectionName}:Port"] = Environment.GetEnvironmentVariable($"{prefix}_CASSANDRA_PORT"),
                [$"{CassandraStorageSettings.SectionName}:UserName"] = Environment.GetEnvironmentVariable($"{prefix}_CASSANDRA_USERNAME"),
                [$"{CassandraStorageSettings.SectionName}:Password"] = Environment.GetEnvironmentVariable($"{prefix}_CASSANDRA_PASSWORD"),
                [$"{CassandraStorageSettings.SectionName}:Keyspace"] = Environment.GetEnvironmentVariable($"{prefix}_CASSANDRA_KEYSPACE"),
                [$"{CassandraStorageSettings.SectionName}:LocalDataCenter"] = Environment.GetEnvironmentVariable($"{prefix}_CASSANDRA_LOCAL_DATA_CENTER")
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            return new CassandraStorageSettings(configuration);
        }
    }
}
