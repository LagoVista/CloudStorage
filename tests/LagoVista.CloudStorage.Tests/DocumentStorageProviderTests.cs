using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using MongoDB.Bson;
using NUnit.Framework;
using System;
using System.Linq;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    public class DocumentStorageProviderTests
    {
        [Test]
        public void Resolve_WithNoProviderSetting_DefaultsToCosmos()
        {
            var settings = DocumentStorageSettingsResolver.Resolve("https://example.documents.azure.com:443/", "key", "TestDb", null);

            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageProviderType.Cosmos));
            Assert.That(settings.Endpoint, Is.EqualTo("https://example.documents.azure.com:443/"));
            Assert.That(settings.SharedKey, Is.EqualTo("key"));
            Assert.That(settings.DatabaseName, Is.EqualTo("TestDb"));
            Assert.That(settings.Mongo, Is.Null);
        }

        [TestCase("cosmos")]
        [TestCase("CosmosDB")]
        [TestCase("azurecosmos")]
        public void ParseProvider_WithCosmosAliases_ReturnsCosmos(string provider)
        {
            Assert.That(DocumentStorageSettingsResolver.ParseProvider(provider), Is.EqualTo(DocumentStorageProviderType.Cosmos));
        }

        [TestCase("mongo")]
        [TestCase("MongoDB")]
        public void ParseProvider_WithMongoAliases_ReturnsMongo(string provider)
        {
            Assert.That(DocumentStorageSettingsResolver.ParseProvider(provider), Is.EqualTo(DocumentStorageProviderType.Mongo));
        }

        [Test]
        public void ParseProvider_WithUnknownProvider_FailsFast()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => DocumentStorageSettingsResolver.ParseProvider("something-else"));
            Assert.That(ex.Message, Does.Contain("Unknown document storage provider"));
        }

        [Test]
        public void Resolve_WithExplicitMongoSettings_KeepsCosmosAndMongoCredentialsSeparate()
        {
            var mongoSettings = new MongoDocumentStorageSettings
            {
                ConnectionString = "mongodb://mongo.example:27017",
                DatabaseName = "MongoTarget"
            };

            var settings = DocumentStorageSettingsResolver.Resolve("https://cosmos.example:443/", "cosmos-key", "LogicalDb", "mongo", mongoSettings);

            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageProviderType.Mongo));
            Assert.That(settings.Endpoint, Is.EqualTo("https://cosmos.example:443/"));
            Assert.That(settings.SharedKey, Is.EqualTo("cosmos-key"));
            Assert.That(settings.DatabaseName, Is.EqualTo("LogicalDb"));
            Assert.That(settings.Mongo.ConnectionString, Is.EqualTo("mongodb://mongo.example:27017"));
            Assert.That(settings.Mongo.DatabaseName, Is.EqualTo("MongoTarget"));
        }

        [TestCase("mongodb://localhost:27017")]
        [TestCase("mongodb+srv://cluster.example")]
        public void Resolve_WithSupportedMongoConnectionString_AcceptsConnectionString(string connectionString)
        {
            var settings = new MongoDocumentStorageSettings
            {
                ConnectionString = connectionString,
                DatabaseName = "MongoTarget"
            };

            var resolved = DocumentStorageSettingsResolver.Resolve(null, null, "LogicalDb", "mongo", settings);
            Assert.That(resolved.Mongo.ConnectionString, Is.EqualTo(connectionString));
        }

        [Test]
        public void Resolve_WithInvalidMongoConnectionString_FailsFast()
        {
            var settings = new MongoDocumentStorageSettings
            {
                ConnectionString = "https://not-mongo.example",
                DatabaseName = "MongoTarget"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => DocumentStorageSettingsResolver.Resolve(null, null, "LogicalDb", "mongo", settings));
            Assert.That(ex.Message, Does.Contain("mongodb:// or mongodb+srv://"));
        }

        [Test]
        public void ResolveMongo_WithGlobalSettings_UsesLogicalDatabaseNameByDefault()
        {
            WithEnvironment(DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariable, "mongodb://global.example:27017", () => WithEnvironment(DocumentStorageSettingsResolver.MongoDatabaseEnvironmentVariable, null, () =>
            {
                var settings = DocumentStorageSettingsResolver.ResolveMongo("LogicalDb");
                Assert.That(settings.ConnectionString, Is.EqualTo("mongodb://global.example:27017"));
                Assert.That(settings.DatabaseName, Is.EqualTo("LogicalDb"));
            }));
        }

        [Test]
        public void ResolveMongo_WithDatabaseSpecificSettings_OverridesGlobalSettings()
        {
            var connectionVariable = DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariablePrefix + "LOGICALDB";
            var databaseVariable = DocumentStorageSettingsResolver.MongoDatabaseEnvironmentVariablePrefix + "LOGICALDB";

            WithEnvironment(DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariable, "mongodb://global.example:27017", () => WithEnvironment(DocumentStorageSettingsResolver.MongoDatabaseEnvironmentVariable, "GlobalDb", () => WithEnvironment(connectionVariable, "mongodb://specific.example:27017", () => WithEnvironment(databaseVariable, "SpecificDb", () =>
            {
                var settings = DocumentStorageSettingsResolver.ResolveMongo("LogicalDb");
                Assert.That(settings.ConnectionString, Is.EqualTo("mongodb://specific.example:27017"));
                Assert.That(settings.DatabaseName, Is.EqualTo("SpecificDb"));
            }))));
        }

        [Test]
        public void ResolveMongo_WithMissingConnectionString_FailsFast()
        {
            var connectionVariable = DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariablePrefix + "LOGICALDB";
            WithEnvironment(DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariable, null, () => WithEnvironment(connectionVariable, null, () =>
            {
                var ex = Assert.Throws<InvalidOperationException>(() => DocumentStorageSettingsResolver.ResolveMongo("LogicalDb"));
                Assert.That(ex.Message, Does.Contain(DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariable));
            }));
        }


        [Test]
        public void MongoSerialization_WithClrId_UsesMongoIdField()
        {
            var document = new MongoIdentityTestDocument { Id = "ABC123", Name = "Test" }.ToBsonDocument();
            Assert.That(document.Contains("_id"), Is.True);
            Assert.That(document["_id"].AsString, Is.EqualTo("ABC123"));
            Assert.That(document.Contains("Id"), Is.False);
        }

        [Test]
        public void DocumentCollectionContract_DoesNotExposeCosmosTypes()
        {
            var cosmosType = typeof(Microsoft.Azure.Cosmos.CosmosClient);
            var methods = typeof(IDocumentCollection).GetMethods();
            Assert.That(methods.Any(method => ContainsType(method.ReturnType, cosmosType.Namespace) || method.GetParameters().Any(parameter => ContainsType(parameter.ParameterType, cosmosType.Namespace))), Is.False);
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

        private static bool ContainsType(Type type, string namespacePrefix)
        {
            if (type.Namespace != null && type.Namespace.StartsWith(namespacePrefix, StringComparison.Ordinal)) return true;
            if (type.IsGenericType && type.GetGenericArguments().Any(argument => ContainsType(argument, namespacePrefix))) return true;
            if (type.HasElementType && ContainsType(type.GetElementType(), namespacePrefix)) return true;
            return false;
        }

        private sealed class MongoIdentityTestDocument
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
