using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests
{
    public class CassandraStorageEnvironmentSettingsTests
    {
        [Test]
        public void Development_ReadsPrefixedEnvironmentVariables()
        {
            WithEnvironment("DEV", () =>
            {
                var settings = CassandraStorageEnvironmentSettings.Development;
                Assert.That(settings.ContactPoints, Is.EqualTo(new[] { "dev-cassandra-0", "dev-cassandra-1" }));
                Assert.That(settings.Port, Is.EqualTo(9142));
                Assert.That(settings.UserName, Is.EqualTo("dev-user"));
                Assert.That(settings.Password, Is.EqualTo("dev-password"));
                Assert.That(settings.Keyspace, Is.EqualTo("nuviot-dev"));
                Assert.That(settings.LocalDataCenter, Is.EqualTo("dc-dev"));
            });
        }

        [Test]
        public void Production_ReadsPrefixedEnvironmentVariables()
        {
            WithEnvironment("PROD", () =>
            {
                var settings = CassandraStorageEnvironmentSettings.Production;
                Assert.That(settings.ContactPoints, Is.EqualTo(new[] { "prod-cassandra-0", "prod-cassandra-1" }));
                Assert.That(settings.Port, Is.EqualTo(9142));
                Assert.That(settings.UserName, Is.EqualTo("prod-user"));
                Assert.That(settings.Password, Is.EqualTo("prod-password"));
                Assert.That(settings.Keyspace, Is.EqualTo("nuviot-prod"));
                Assert.That(settings.LocalDataCenter, Is.EqualTo("dc-prod"));
            });
        }

        private static void WithEnvironment(string prefix, Action action)
        {
            var values = new Dictionary<string, string>
            {
                [$"{prefix}_CASSANDRA_CONTACT_POINTS"] = $"{prefix.ToLowerInvariant()}-cassandra-0,{prefix.ToLowerInvariant()}-cassandra-1",
                [$"{prefix}_CASSANDRA_PORT"] = "9142",
                [$"{prefix}_CASSANDRA_USERNAME"] = $"{prefix.ToLowerInvariant()}-user",
                [$"{prefix}_CASSANDRA_PASSWORD"] = $"{prefix.ToLowerInvariant()}-password",
                [$"{prefix}_CASSANDRA_KEYSPACE"] = $"nuviot-{prefix.ToLowerInvariant()}",
                [$"{prefix}_CASSANDRA_LOCAL_DATA_CENTER"] = $"dc-{prefix.ToLowerInvariant()}"
            };

            var originals = new Dictionary<string, string>();
            try
            {
                foreach (var value in values)
                {
                    originals[value.Key] = Environment.GetEnvironmentVariable(value.Key);
                    Environment.SetEnvironmentVariable(value.Key, value.Value);
                }

                action();
            }
            finally
            {
                foreach (var original in originals)
                {
                    Environment.SetEnvironmentVariable(original.Key, original.Value);
                }
            }
        }
    }
}
