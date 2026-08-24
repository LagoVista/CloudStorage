using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentStorageClientTests
    {
        [Test]
        public void Settings_DefaultToCosmos_UsingStandardSectionRequirements()
        {
            var configuration = Build(new Dictionary<string, string>
            {
                ["DefaultDocDBStorage:Endpoint"] = "https://localhost:8081/",
                ["DefaultDocDBStorage:AccessKey"] = "cosmos-key",
                ["DefaultDocDBStorage:DbName"] = "Nuviot"
            });

            var settings = new DocumentStorageClientSettings(configuration);

            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageProviderType.Cosmos));
            Assert.That(settings.Endpoint, Is.EqualTo("https://localhost:8081/"));
            Assert.That(settings.AccessKey, Is.EqualTo("cosmos-key"));
            Assert.That(settings.DatabaseName, Is.EqualTo("Nuviot"));
            Assert.That(settings.MongoConnectionString, Is.Null);
        }

        [Test]
        public void Settings_SelectMongo_UsingMongoConnectionSection()
        {
            var configuration = Build(new Dictionary<string, string>
            {
                ["DefaultDocDBStorage:Provider"] = "Mongo",
                ["DefaultDocDBStorage:DbName"] = "Nuviot",
                ["DefaultDocDBStorage:MongoDbName"] = "nuviot-dev",
                ["MongoDocumentStorage:Hosts"] = "mongo-0.mongo.svc,mongo-1.mongo.svc",
                ["MongoDocumentStorage:UserName"] = "mongo-app",
                ["MongoDocumentStorage:Password"] = "secret",
                ["MongoDocumentStorage:AuthenticationDatabase"] = "admin"
            });

            var settings = new DocumentStorageClientSettings(configuration);

            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageProviderType.Mongo));
            Assert.That(settings.DatabaseName, Is.EqualTo("Nuviot"));
            Assert.That(settings.MongoDatabaseName, Is.EqualTo("nuviot-dev"));
            Assert.That(settings.MongoConnectionString, Is.EqualTo("mongodb://mongo-app:secret@mongo-0.mongo.svc:27017,mongo-1.mongo.svc:27017/?authSource=admin"));
        }

        [Test]
        public void Provider_ReturnsCosmosClient_WhenCosmosSelected()
        {
            var cosmos = new FakeCosmosClient();
            var mongo = new FakeMongoClient();
            var provider = new DocumentStorageClientProvider(new FakeSettings(DocumentStorageProviderType.Cosmos), cosmos, mongo);

            Assert.That(provider.GetClient(), Is.SameAs(cosmos));
        }

        [Test]
        public void Provider_ReturnsMongoClient_WhenMongoSelected()
        {
            var cosmos = new FakeCosmosClient();
            var mongo = new FakeMongoClient();
            var provider = new DocumentStorageClientProvider(new FakeSettings(DocumentStorageProviderType.Mongo), cosmos, mongo);

            Assert.That(provider.GetClient(), Is.SameAs(mongo));
        }

        private static IConfiguration Build(IDictionary<string, string> values) =>
            new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        private sealed class FakeSettings : IDocumentStorageClientSettings
        {
            public FakeSettings(DocumentStorageProviderType provider) => Provider = provider;
            public DocumentStorageProviderType Provider { get; }
            public string Endpoint => null;
            public string AccessKey => null;
            public string DatabaseName => "test";
            public string MongoConnectionString => null;
            public string MongoDatabaseName => null;
        }

        private abstract class FakeClient : IDocumentStorageClient
        {
            public Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<OperationResponse<TEntity>> UpsertDocumentAsync<TEntity>(TEntity item) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<TEntity> GetDocumentAsync<TEntity>(string id, bool throwOnNotFound = true) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<TEntity> GetDocumentAsync<TEntity>(string id, string partitionKey, bool throwOnNotFound = true) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id, string partitionKey) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(string entityType, DocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class => throw new NotSupportedException();
        }

        private sealed class FakeCosmosClient : FakeClient, ICosmosDocumentStorageClient
        {
        }

        private sealed class FakeMongoClient : FakeClient, IMongoDocumentStorageClient
        {
        }
    }
}
