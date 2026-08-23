using LagoVista.CloudStorage.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    public class TestConnectionsTests
    {
        [Test]
        public void ProductionMongoDocumentStorage_ReadsProductionEnvironmentVariables()
        {
            WithMongoEnvironment("PROD", () =>
            {
                var settings = TestConnections.ProductionMongoDocumentStorage;
                Assert.That(settings.Hosts, Is.EqualTo(new[] { "mongo-0.mongo.svc", "mongo-1.mongo.svc" }));
                Assert.That(settings.Port, Is.EqualTo(27018));
                Assert.That(settings.UserName, Is.EqualTo("prod-user"));
                Assert.That(settings.Password, Is.EqualTo("prod-password"));
                Assert.That(settings.AuthenticationDatabase, Is.EqualTo("admin-prod"));
                Assert.That(settings.ReplicaSet, Is.EqualTo("rs-prod"));
                Assert.That(settings.UseTls, Is.True);
                Assert.That(settings.BuildConnectionString(), Is.EqualTo("mongodb://prod-user:prod-password@mongo-0.mongo.svc:27018,mongo-1.mongo.svc:27018/?authSource=admin-prod&replicaSet=rs-prod&tls=true"));
            });
        }

        [Test]
        public void TestMongoDocumentStorage_ReadsTestEnvironmentVariablesAndDefaults()
        {
            WithMongoEnvironment("TEST", () =>
            {
                Environment.SetEnvironmentVariable("TEST_MONGO_PORT", null);
                Environment.SetEnvironmentVariable("TEST_MONGO_AUTHENTICATION_DATABASE", null);
                Environment.SetEnvironmentVariable("TEST_MONGO_REPLICA_SET", null);
                Environment.SetEnvironmentVariable("TEST_MONGO_USE_TLS", null);

                var settings = TestConnections.TestMongoDocumentStorage;
                Assert.That(settings.Hosts, Is.EqualTo(new[] { "mongo-0.mongo.svc", "mongo-1.mongo.svc" }));
                Assert.That(settings.Port, Is.EqualTo(27017));
                Assert.That(settings.AuthenticationDatabase, Is.EqualTo("admin"));
                Assert.That(settings.ReplicaSet, Is.Null);
                Assert.That(settings.UseTls, Is.False);
            });
        }

        private static void WithMongoEnvironment(string prefix, Action action)
        {
            var values = new Dictionary<string, string>
            {
                [$"{prefix}_MONGO_HOSTS"] = "mongo-0.mongo.svc,mongo-1.mongo.svc",
                [$"{prefix}_MONGO_PORT"] = "27018",
                [$"{prefix}_MONGO_USERNAME"] = prefix == "PROD" ? "prod-user" : "test-user",
                [$"{prefix}_MONGO_PASSWORD"] = prefix == "PROD" ? "prod-password" : "test-password",
                [$"{prefix}_MONGO_AUTHENTICATION_DATABASE"] = prefix == "PROD" ? "admin-prod" : "admin-test",
                [$"{prefix}_MONGO_REPLICA_SET"] = prefix == "PROD" ? "rs-prod" : "rs-test",
                [$"{prefix}_MONGO_USE_TLS"] = "true"
            };

            var priorValues = new Dictionary<string, string>();
            foreach (var pair in values)
            {
                priorValues[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            try
            {
                action();
            }
            finally
            {
                foreach (var pair in priorValues) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }
}
