using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentStorageSettingsProviderTests
    {
        [Test]
        public void CosmosDefault_UsesDefaultDocDbSectionWithoutMongoConfiguration()
        {
            var configuration = Build(new Dictionary<string, string>
            {
                ["DefaultDocDBStorage:Endpoint"] = "https://localhost:8081/",
                ["DefaultDocDBStorage:AccessKey"] = "cosmos-key",
                ["DefaultDocDBStorage:DbName"] = "Nuviot"
            });

            var settings = new DocumentStorageSettingsProvider(configuration).Default;

            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageProviderType.Cosmos));
            Assert.That(settings.Endpoint, Is.EqualTo("https://localhost:8081/"));
            Assert.That(settings.SharedKey, Is.EqualTo("cosmos-key"));
            Assert.That(settings.DatabaseName, Is.EqualTo("Nuviot"));
            Assert.That(settings.Mongo, Is.Null);
        }

        [Test]
        public void MongoSelected_UsesDefaultDocDbProviderAndMongoConnectionSection()
        {
            var configuration = Build(new Dictionary<string, string>
            {
                ["DefaultDocDBStorage:Provider"] = "Mongo",
                ["DefaultDocDBStorage:DbName"] = "Nuviot",
                ["DefaultDocDBStorage:MongoDbName"] = "nuviot-dev",
                ["MongoDocumentStorage:Hosts"] = "mongo-0.mongo.svc,mongo-1.mongo.svc",
                ["MongoDocumentStorage:Port"] = "27017",
                ["MongoDocumentStorage:UserName"] = "mongo-app",
                ["MongoDocumentStorage:Password"] = "secret",
                ["MongoDocumentStorage:AuthenticationDatabase"] = "admin"
            });

            var settings = new DocumentStorageSettingsProvider(configuration).Default;

            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageProviderType.Mongo));
            Assert.That(settings.DatabaseName, Is.EqualTo("Nuviot"));
            Assert.That(settings.Mongo, Is.Not.Null);
            Assert.That(settings.Mongo.DatabaseName, Is.EqualTo("nuviot-dev"));
            Assert.That(settings.Mongo.ConnectionString, Is.EqualTo("mongodb://mongo-app:secret@mongo-0.mongo.svc:27017,mongo-1.mongo.svc:27017/?authSource=admin"));
        }

        private static IConfiguration Build(IDictionary<string, string> values) =>
            new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
