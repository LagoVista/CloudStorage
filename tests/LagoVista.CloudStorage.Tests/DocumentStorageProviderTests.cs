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
        public void DocumentCollectionFactory_WithDefaultProvider_ReturnsCosmosAdapter()
        {
            var variableName = DocumentStorageSettingsResolver.ProviderEnvironmentVariablePrefix + "TESTDB";
            var priorValue = Environment.GetEnvironmentVariable(variableName);
            try
            {
                Environment.SetEnvironmentVariable(variableName, null);
                var factory = new DocumentCollectionFactory(CosmosClientProvider.Shared);
                var collection = factory.Create("https://example.documents.azure.com:443/", "key", "TestDb");
                Assert.That(collection, Is.TypeOf<CosmosDocumentCollection>());
            }
            finally
            {
                Environment.SetEnvironmentVariable(variableName, priorValue);
            }
        }

        [Test]
        public void DocumentCollectionFactory_WithMongoProvider_ReturnsMongoAdapter()
        {
            var variableName = DocumentStorageSettingsResolver.ProviderEnvironmentVariablePrefix + "TESTDB";
            var priorValue = Environment.GetEnvironmentVariable(variableName);
            try
            {
                Environment.SetEnvironmentVariable(variableName, "mongo");
                var factory = new DocumentCollectionFactory(CosmosClientProvider.Shared);
                var collection = factory.Create("mongodb://localhost:27017", null, "TestDb");
                Assert.That(collection, Is.TypeOf<MongoDocumentCollection>());
            }
            finally
            {
                Environment.SetEnvironmentVariable(variableName, priorValue);
            }
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
