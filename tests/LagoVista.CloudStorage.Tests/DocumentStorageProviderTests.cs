using LagoVista.CloudStorage.DocumentDB;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentStorageProviderTests
    {
        [Test]
        public void Resolve_WithNoProviderSetting_DefaultsToCosmos()
        {
            var settings = DocumentStorageSettingsResolver.Resolve(
                "https://example.documents.azure.com:443/",
                "key",
                "TestDb",
                null);

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
            Assert.That(
                DocumentStorageSettingsResolver.ParseProvider(provider),
                Is.EqualTo(DocumentStorageProviderType.Cosmos));
        }

        [TestCase("mongo")]
        [TestCase("MongoDB")]
        public void ParseProvider_WithMongoAliases_ReturnsMongo(string provider)
        {
            Assert.That(
                DocumentStorageSettingsResolver.ParseProvider(provider),
                Is.EqualTo(DocumentStorageProviderType.Mongo));
        }

        [Test]
        public void ParseProvider_WithUnknownProvider_FailsFast()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                DocumentStorageSettingsResolver.ParseProvider("something-else"));

            Assert.That(ex.Message, Does.Contain("Unknown document storage provider"));
        }
    }
}
