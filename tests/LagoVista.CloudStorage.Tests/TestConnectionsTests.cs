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
        public void TestMongoDocumentStorage_UsesDeterministicLocalDockerSettings()
        {
            var settings = TestConnections.TestMongoDocumentStorage;
            Assert.That(settings.Hosts, Is.EqualTo(new[] { "localhost" }));
            Assert.That(settings.Port, Is.EqualTo(27018));
            Assert.That(settings.UserName, Is.EqualTo("nuviot-test"));
            Assert.That(settings.Password, Is.EqualTo("nuviot-test-password"));
            Assert.That(settings.AuthenticationDatabase, Is.EqualTo("admin"));
            Assert.That(settings.ReplicaSet, Is.Null);
            Assert.That(settings.UseTls, Is.False);
            Assert.That(settings.BuildConnectionString(), Is.EqualTo("mongodb://nuviot-test:nuviot-test-password@localhost:27018/?authSource=admin"));
        }

        private static void WithMongoEnvironment(string prefix, Action action)
        {
            var values = new Dictionary<string, string>
            {
                [$"{prefix}_MONGO_HOSTS"] = "mongo-0.mongo.svc,mongo-1.mongo.svc",
                [$"{prefix}_MONGO_PORT"] = "27018",
                [$"{prefix}_MONGO_USERNAME"] = "prod-user",
                [$"{prefix}_MONGO_PASSWORD"] = "prod-password",
                [$"{prefix}_MONGO_AUTHENTICATION_DATABASE"] = "admin-prod",
                [$"{prefix}_MONGO_REPLICA_SET"] = "rs-prod",
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
