using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Mongo")]
    [TestCategory("MongoDocumentStorageDepth")]
    public sealed class MongoDocumentStorageClientCoverageCleanupIntegrationTests
    {
        private MongoDocumentStorageConnectionSettings _settings;
        private MongoClient _cleanupClient;
        private MongoDocumentStorageClient _client;

        [TestInitialize]
        public void Setup()
        {
            _settings = new MongoDocumentStorageConnectionSettings
            {
                Hosts = new List<string> { "localhost" }.AsReadOnly(),
                Port = 27018,
                UserName = "nuviot-test",
                Password = "nuviot-test-password",
                AuthenticationDatabase = "admin",
                DatabaseName = $"doc_cleanup_{Guid.NewGuid():N}"
            };

            var factory = new MongoStorageClientFactory();
            _client = new MongoDocumentStorageClient(_settings, new DocumentCollectionNameResolver(), factory);
            _cleanupClient = new MongoClient(_settings.BuildConnectionString());
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            if (_cleanupClient != null && _settings != null && !String.IsNullOrWhiteSpace(_settings.DatabaseName)) await _cleanupClient.DropDatabaseAsync(_settings.DatabaseName);
        }

        [TestMethod]
        public async Task GenericDeleteOverload_DeletesDocument()
        {
            var entity = CreateEntity("delete-overload", "Delete Overload");
            await _client.CreateDocumentAsync(entity);

            var deleted = await _client.DeleteDocumentAsync<MongoCleanupDocumentEntity>(entity.Id.Value);
            Assert.IsNotNull(deleted);
            Assert.IsNull(await _client.GetDocumentAsync<MongoCleanupDocumentEntity>(entity.Id.Value, throwOnNotFound: false));
        }

        [TestMethod]
        public async Task KnownQueries_TypedResultsDeserializeFromBson()
        {
            var id = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var entityType = nameof(MongoCleanupDocumentEntity);
            var document = new JObject
            {
                ["id"] = id,
                ["EntityType"] = entityType,
                ["Key"] = $"cleanup-{id.ToLowerInvariant()}",
                ["Name"] = "Typed Known Query",
                ["OwnerOrganization"] = new JObject { ["Id"] = "ORG1", ["Text"] = "Organization One" },
                ["Status"] = new JObject { ["Id"] = "READY", ["Text"] = "Ready" },
                ["Detail"] = null
            };

            await _client.UpsertRawDocumentAsync(entityType, id, document.ToString());

            var statusRequest = new DocumentQueryRequest(DocumentQueryType.EntityUtilsDocumentsByStatusIds)
                .WithParameter("entityType", entityType)
                .WithParameter("orgId", "ORG1")
                .WithParameter("statusIds", new List<string> { "READY" })
                .WithParameter("maxItems", 10);

            var byStatus = (await _client.QueryKnownAsync<MongoCleanupKnownResult>(entityType, statusRequest)).ToList();
            Assert.AreEqual(1, byStatus.Count);
            Assert.AreEqual(id, byStatus[0].Id);
            Assert.AreEqual("READY", byStatus[0].Status?.Id);

            var emptyFieldRequest = new DocumentQueryRequest(DocumentQueryType.EntityUtilsDocumentsWithEmptyField)
                .WithParameter("entityType", entityType)
                .WithParameter("orgId", "ORG1")
                .WithParameter("fieldName", "Detail")
                .WithParameter("maxItems", 10);

            var emptyField = (await _client.QueryKnownAsync<MongoCleanupKnownResult>(entityType, emptyFieldRequest)).ToList();
            Assert.AreEqual(1, emptyField.Count);
            Assert.AreEqual(id, emptyField[0].Id);
        }

        [TestMethod]
        public async Task FallbackJObjectProjection_ReadsTypedDocumentWithoutEntityType()
        {
            var entity = CreateEntity("fallback-projection", "Fallback Projection");
            await _client.CreateDocumentAsync(entity);

            var projection = await _client.GetDocumentProjectionAsync<JObject>(entity.Id.Value);
            Assert.IsNotNull(projection);
            Assert.AreEqual(entity.Id.Value, projection.Value<string>("id"));
            Assert.AreEqual(nameof(MongoCleanupDocumentEntity), projection.Value<string>("EntityType"));
            Assert.AreEqual("fallback-projection", projection.Value<string>(nameof(MongoCleanupDocumentEntity.Detail)));

            Assert.IsNull(await _client.GetDocumentProjectionAsync<JObject>(Guid.NewGuid().ToString("N").ToUpperInvariant(), throwOnNotFound: false));
            await Assert.ThrowsExactlyAsync<RecordNotFoundException>(() => _client.GetDocumentProjectionAsync<JObject>(Guid.NewGuid().ToString("N").ToUpperInvariant()));
        }

        private static MongoCleanupDocumentEntity CreateEntity(string detail, string name)
        {
            var id = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return new MongoCleanupDocumentEntity
            {
                Id = id,
                Key = $"cleanup-{id.ToLowerInvariant()}",
                Name = name,
                EntityType = nameof(MongoCleanupDocumentEntity),
                OwnerOrganization = EntityHeader.Create("ORG1", "Organization One"),
                Detail = detail
            };
        }
    }

    internal sealed class MongoCleanupDocumentEntity : EntityBase
    {
        public string Detail { get; set; }
    }

    [BsonIgnoreExtraElements]
    internal sealed class MongoCleanupKnownResult
    {
        [BsonId]
        public string Id { get; set; }
        public string EntityType { get; set; }
        public string Name { get; set; }
        public EntityHeader Status { get; set; }
        public string Detail { get; set; }
    }
}
