using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
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
        public void ProviderSettings_DefaultToCosmos()
        {
            var configuration = Build(new Dictionary<string, string>());
            var settings = new DocumentStorageProviderSettings(configuration);
            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageClientType.Cosmos));
        }

        [Test]
        public void ProviderSettings_SelectMongo()
        {
            var configuration = Build(new Dictionary<string, string>
            {
                ["DefaultDocDBStorage:Provider"] = "Mongo"
            });

            var settings = new DocumentStorageProviderSettings(configuration);
            Assert.That(settings.Provider, Is.EqualTo(DocumentStorageClientType.Mongo));
        }

        [Test]
        public void CosmosSettings_UseExplicitCosmosSectionValues()
        {
            var configuration = Build(new Dictionary<string, string>
            {
                ["DefaultDocDBStorage:Endpoint"] = "https://localhost:8081/",
                ["DefaultDocDBStorage:AccessKey"] = "cosmos-key",
                ["DefaultDocDBStorage:DbName"] = "Nuviot"
            });

            var settings = new CosmosConnectionSettings(configuration);
            Assert.That(settings.Endpoint, Is.EqualTo("https://localhost:8081/"));
            Assert.That(settings.AccessKey, Is.EqualTo("cosmos-key"));
            Assert.That(settings.DatabaseName, Is.EqualTo("Nuviot"));
        }

        [Test]
        public void MongoSettings_UseExplicitMongoSectionValues()
        {
            var configuration = Build(new Dictionary<string, string>
            {
                ["MongoDocumentStorage:Hosts"] = "mongo-0.mongo.svc,mongo-1.mongo.svc",
                ["MongoDocumentStorage:UserName"] = "mongo-app",
                ["MongoDocumentStorage:Password"] = "secret",
                ["MongoDocumentStorage:AuthenticationDatabase"] = "admin",
                ["MongoDocumentStorage:DatabaseName"] = "nuviot-dev"
            });

            var settings = new MongoConnectionSettings(configuration);
            Assert.That(settings.DatabaseName, Is.EqualTo("nuviot-dev"));
            Assert.That(settings.ConnectionString, Is.EqualTo("mongodb://mongo-app:secret@mongo-0.mongo.svc:27017,mongo-1.mongo.svc:27017/?authSource=admin"));
        }

        [Test]
        public void Provider_ReturnsCosmosClient_WhenCosmosSelected()
        {
            var cosmos = new FakeCosmosClient();
            var mongo = new FakeMongoClient();
            var provider = new DocumentStorageClientProvider(new FakeSettings(DocumentStorageClientType.Cosmos), cosmos, mongo);
            Assert.That(provider.GetClient(), Is.SameAs(cosmos));
        }

        [Test]
        public void Provider_ReturnsMongoClient_WhenMongoSelected()
        {
            var cosmos = new FakeCosmosClient();
            var mongo = new FakeMongoClient();
            var provider = new DocumentStorageClientProvider(new FakeSettings(DocumentStorageClientType.Mongo), cosmos, mongo);
            Assert.That(provider.GetClient(), Is.SameAs(mongo));
        }

        private static IConfiguration Build(IDictionary<string, string> values) => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        private sealed class FakeSettings : IDocumentStorageProviderSettings
        {
            public FakeSettings(DocumentStorageClientType provider) => Provider = provider;
            public DocumentStorageClientType Provider { get; }
        }

        private abstract class FakeClient : IDocumentStorageClient
        {
            public Task<OperationResponse<TEntity>> CreateDocumentAsync<TEntity>(TEntity item) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<OperationResponse<TEntity>> UpsertDocumentAsync<TEntity>(TEntity item, string eTag = null) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<TEntity> GetDocumentAsync<TEntity>(string id, bool throwOnNotFound = true) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<TEntity> GetDocumentAsync<TEntity>(string id, string partitionKey, bool throwOnNotFound = true) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<TProjection> GetDocumentProjectionAsync<TProjection>(string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TProjection : class => throw new NotSupportedException();
            public Task<TProjection> GetDocumentProjectionAsync<TProjection>(string entityType, string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TProjection : class => throw new NotSupportedException();
            public Task<IEnumerable<TProjection>> GetDocumentProjectionsAsync<TProjection>(string entityType, Expression<Func<TProjection, bool>> query, CancellationToken cancellationToken = default) where TProjection : class => throw new NotSupportedException();
            public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<OperationResponse<TEntity>> DeleteDocumentAsync<TEntity>(string id, string partitionKey) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<OperationResponse<TEntity>> PatchDocumentAsync<TEntity>(PatchRequest request) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<ListResponse<TEntity>> QueryAsync<TEntity>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, string>> sort, ListRequest listRequest, bool descending) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<ListResponse<TEntity>> QueryAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> query, Expression<Func<TEntity, TKey>> sort, ListRequest listRequest, bool descending) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<ListResponse<TEntity>> QueryAllAsync<TEntity>(Expression<Func<TEntity, bool>> query, ListRequest listRequest) where TEntity : class, IIDEntity, IKeyedEntity, IOwnedEntity, INamedEntity, INoSQLEntity, IAuditableEntity => throw new NotSupportedException();
            public Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(string entityType, DocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class => throw new NotSupportedException();
        }

        private sealed class FakeCosmosClient : FakeClient, ICosmosDocumentStorageClient { }
        private sealed class FakeMongoClient : FakeClient, IMongoDocumentStorageClient { }
    }
}
