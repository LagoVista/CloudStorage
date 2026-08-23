using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    public class RichMongoProviderTests
    {
        [Test]
        public void DocumentStorageFactory_WithMongoSettings_CreatesRichProviderUsingDomainCollection()
        {
            var settings = new DocumentStorageSettings
            {
                Provider = DocumentStorageProviderType.Mongo,
                DatabaseName = "LogicalDb",
                Mongo = new MongoDocumentStorageSettings
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "MongoTarget"
                }
            };

            var storage = DocumentStorageFactory.Create<RichMongoTestEntity>(settings, null);

            Assert.That(storage.GetCollectionName(), Is.EqualTo("RichMongoDomain"));
            Assert.That(storage.GetPartitionKey(), Is.Null);
        }

        [Test]
        public void DocumentStorageFactory_ResolveAndCreate_WithDatabaseMongoOverride_CreatesMongoProvider()
        {
            var providerVariable = DocumentStorageSettingsResolver.ProviderEnvironmentVariablePrefix + "LOGICALDB";
            var connectionVariable = DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariablePrefix + "LOGICALDB";
            var databaseVariable = DocumentStorageSettingsResolver.MongoDatabaseEnvironmentVariablePrefix + "LOGICALDB";

            WithEnvironment(providerVariable, "mongo", () => WithEnvironment(connectionVariable, "mongodb://localhost:27017", () => WithEnvironment(databaseVariable, "MongoTarget", () =>
            {
                var storage = DocumentStorageFactory.ResolveAndCreate<RichMongoTestEntity>("https://cosmos.example:443/", "cosmos-key", "LogicalDb", null);
                Assert.That(storage.GetCollectionName(), Is.EqualTo("RichMongoDomain"));
                Assert.That(storage.GetPartitionKey(), Is.Null);
            })));
        }

        [Test]
        public void DocumentStorageFactory_ResolveAndCreate_WithNoProviderSetting_CreatesCosmosProvider()
        {
            var providerVariable = DocumentStorageSettingsResolver.ProviderEnvironmentVariablePrefix + "LOGICALDB";
            WithEnvironment(providerVariable, null, () => WithEnvironment(DocumentStorageSettingsResolver.ProviderEnvironmentVariable, null, () =>
            {
                var storage = DocumentStorageFactory.ResolveAndCreate<RichMongoTestEntity>("https://cosmos.example:443/", "cosmos-key", "LogicalDb", null);
                Assert.That(storage.GetCollectionName(), Is.EqualTo("LogicalDb_Collections"));
                Assert.That(storage.GetPartitionKey(), Is.EqualTo("/_partitionKey"));
            }));
        }

        [Test]
        public void OperationResponse_WithProviderNeutralResource_ReturnsResource()
        {
            var entity = new RichMongoTestEntity { Id = "ABC123", Name = "Test" };
            var response = new OperationResponse<RichMongoTestEntity>(entity);
            Assert.That(response.Resource, Is.SameAs(entity));
        }

        private static void WithEnvironment(string variableName, string value, Action action)
        {
            var priorValue = Environment.GetEnvironmentVariable(variableName);
            try
            {
                Environment.SetEnvironmentVariable(variableName, value);
                action();
            }
            finally
            {
                Environment.SetEnvironmentVariable(variableName, priorValue);
            }
        }

        [EntityDescription("RichMongoDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(RichMongoProviderTests))]
        private sealed class RichMongoTestEntity : EntityBase
        {
        }
    }
}
