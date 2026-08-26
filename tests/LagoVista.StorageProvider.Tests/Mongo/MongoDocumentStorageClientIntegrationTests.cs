using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [TestCategory("Mongo")]
    public class MongoDocumentStorageClientIntegrationTests
    {
        private MongoDocumentStorageConnectionSettings _settings;
        private MongoStorageClientFactory _clientFactory;
        private MongoDocumentStorageClient _client;

        [TestInitialize]
        public void Initialize()
        {
            _settings = new MongoDocumentStorageConnectionSettings
            {
                Hosts = new[] { "localhost" },
                Port = 27018,
                UserName = "nuviot-test",
                Password = "nuviot-test-password",
                AuthenticationDatabase = "admin",
                DatabaseName = $"cloudstorage_coverage_{Guid.NewGuid():N}",
                UseTls = false
            };

            _clientFactory = new MongoStorageClientFactory();
            _client = new MongoDocumentStorageClient(
                _settings,
                new DocumentCollectionNameResolver(),
                _clientFactory);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            if (_clientFactory != null && _settings != null)
            {
                await _clientFactory
                    .GetClient(_settings.BuildConnectionString())
                    .DropDatabaseAsync(_settings.DatabaseName);
            }
        }

        [TestMethod]
        public async Task CreateGetDeleteRoundTrip()
        {
            var entity = CreateEntity("crud", "initial");

            var created = await _client.CreateDocumentAsync(entity);
            Assert.IsNotNull(created);
            Assert.IsFalse(String.IsNullOrWhiteSpace(entity.ETag));

            var loaded = await _client.GetDocumentAsync<CoverageEntity>(entity.Id);
            Assert.AreEqual(entity.Id, loaded.Id);
            Assert.AreEqual("initial", loaded.Marker);
            Assert.AreEqual(entity.ETag, loaded.ETag);

            var deleted = await _client.DeleteDocumentAsync<CoverageEntity>(entity.Id);
            Assert.IsNotNull(deleted);

            var missing = await _client.GetDocumentAsync<CoverageEntity>(entity.Id, throwOnNotFound: false);
            Assert.IsNull(missing);
        }

        [TestMethod]
        public async Task DuplicateCreateReportsContentModified()
        {
            var entity = CreateEntity("duplicate", "first");
            await _client.CreateDocumentAsync(entity);

            var duplicate = CreateEntity("duplicate-copy", "second");
            duplicate.Id = entity.Id;

            await Assert.ThrowsExceptionAsync<ContentModifiedException>(
                () => _client.CreateDocumentAsync(duplicate));
        }

        [TestMethod]
        public async Task UpsertWithETagEnforcesOptimisticConcurrency()
        {
            var entity = CreateEntity("etag", "v1");
            await _client.CreateDocumentAsync(entity);
            var originalETag = entity.ETag;

            entity.Marker = "v2";
            await _client.UpsertDocumentAsync(entity, originalETag);
            var currentETag = entity.ETag;

            Assert.AreNotEqual(originalETag, currentETag);

            entity.Marker = "stale-write";
            await Assert.ThrowsExceptionAsync<ContentModifiedException>(
                () => _client.UpsertDocumentAsync(entity, originalETag));

            var loaded = await _client.GetDocumentAsync<CoverageEntity>(entity.Id);
            Assert.AreEqual("v2", loaded.Marker);
            Assert.AreEqual(currentETag, loaded.ETag);
        }

        [TestMethod]
        public async Task PatchUpdatesDocumentAndRefreshesETag()
        {
            var entity = CreateEntity("patch", "before");
            await _client.CreateDocumentAsync(entity);
            var originalETag = entity.ETag;

            var patched = await _client.PatchDocumentAsync<CoverageEntity>(new PatchRequest
            {
                Id = entity.Id,
                ETag = originalETag,
                Steps = new[]
                {
                    new PatchStep
                    {
                        Op = PatchOp.Set,
                        LogicalPath = nameof(CoverageEntity.Marker),
                        Value = new JValue("after")
                    }
                }
            });

            Assert.IsNotNull(patched?.Result);
            Assert.AreEqual("after", patched.Result.Marker);
            Assert.AreNotEqual(originalETag, patched.Result.ETag);

            await Assert.ThrowsExceptionAsync<ContentModifiedException>(() =>
                _client.PatchDocumentAsync<CoverageEntity>(new PatchRequest
                {
                    Id = entity.Id,
                    ETag = originalETag,
                    Steps = new[]
                    {
                        new PatchStep
                        {
                            Op = PatchOp.Set,
                            LogicalPath = nameof(CoverageEntity.Marker),
                            Value = new JValue("stale")
                        }
                    }
                }));
        }

        [TestMethod]
        public async Task QueryFiltersToMatchingEntities()
        {
            await _client.CreateDocumentAsync(CreateEntity("query-a", "target"));
            await _client.CreateDocumentAsync(CreateEntity("query-b", "other"));
            await _client.CreateDocumentAsync(CreateEntity("query-c", "target"));

            var results = (await _client.QueryAsync<CoverageEntity>(item => item.Marker == "target")).ToList();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.All(item => item.Marker == "target"));
        }

        [TestMethod]
        public async Task DeleteMissingDocumentReportsNotFound()
        {
            var id = new CoverageEntity().Id;
            await Assert.ThrowsExceptionAsync<RecordNotFoundException>(
                () => _client.DeleteDocumentAsync<CoverageEntity>(id));
        }

        private static CoverageEntity CreateEntity(string key, string marker)
        {
            return new CoverageEntity
            {
                EntityType = nameof(CoverageEntity),
                Name = $"Coverage {key}",
                Key = key,
                Marker = marker
            };
        }

        private sealed class CoverageEntity : EntityBase
        {
            public CoverageEntity()
            {
                EntityType = nameof(CoverageEntity);
            }

            public string Marker { get; set; }
        }
    }
}
