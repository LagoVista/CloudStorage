using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Mongo")]
    [TestCategory("ScratchData")]
    public sealed class MongoScratchStoreIntegrationTests
    {
        private const string UserName = "nuviot-test";
        private const string Password = "nuviot-test-password";
        private const int Port = 27018;

        private IMongoClient _mongoClient;
        private ServiceProvider _serviceProvider;
        private IScratchStore _store;
        private string _databaseName;

        [TestInitialize]
        public void Setup()
        {
            _databaseName = $"scratch_tests_{Guid.NewGuid():N}";
            var settings = new TestScratchStorageSettings(_databaseName);

            var services = new ServiceCollection();
            services.ConfigureScratchData<ScratchRecord>(definition =>
            {
                definition.Index(x => x.Session.Id);
                definition.RetainFor(TimeSpan.FromMinutes(5));
            });

            _serviceProvider = services.BuildServiceProvider();
            var clientFactory = new MongoStorageClientFactory();
            _store = new MongoScratchStore(settings, clientFactory, _serviceProvider);
            _mongoClient = clientFactory.GetClient(settings.BuildConnectionString());
        }

        [TestCleanup]
        public async Task TearDownAsync()
        {
            if (_mongoClient != null && !String.IsNullOrWhiteSpace(_databaseName))
                await _mongoClient.DropDatabaseAsync(_databaseName);

            _serviceProvider?.Dispose();
        }

        [TestMethod]
        public async Task UpsertQueryAndProviderOwnedTtl_WorkEndToEnd()
        {
            var organization = EntityHeader.Create("ORG2", "Organization Two");
            var record = new ScratchRecord
            {
                Id = NormalizedId32.Factory(),
                Organization = organization,
                Session = EntityHeader.Create("SESSION1", "Session One"),
                Value = "first"
            };

            await _store.UpsertAsync(record);
            record.Value = "second";
            await _store.UpsertAsync(record);

            var loaded = await _store.GetAsync<ScratchRecord>(new StorageKey(record.Id.Value, organization.Id));
            Assert.IsNotNull(loaded);
            Assert.AreEqual("second", loaded.Value);

            var page = await _store.QueryAsync(new StorageQuery<ScratchRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Session.Id, StorageFilterOperator.Equal, "SESSION1"));
            Assert.IsTrue(page.Items.Any(x => x.Id.Value == record.Id.Value));

            var rawCollection = _mongoClient.GetDatabase(_databaseName)
                .GetCollection<BsonDocument>(StorageRecordIdentity.GetCollectionName<ScratchRecord>());
            var raw = await rawCollection.Find(new BsonDocument("_id", record.Id.Value)).FirstOrDefaultAsync();
            Assert.IsNotNull(raw);
            Assert.IsTrue(raw.Contains("_storageExpiresUtc"));
            Assert.AreEqual(BsonType.DateTime, raw["_storageExpiresUtc"].BsonType);

            var indexes = (await rawCollection.Indexes.ListAsync()).ToList().Select(x => x["name"].AsString).ToList();
            CollectionAssert.Contains(indexes, "ix_organization_id");
            CollectionAssert.Contains(indexes, "ix_session_id");
            CollectionAssert.Contains(indexes, "ix_storage_expires_utc");

            await _store.DeleteAsync<ScratchRecord>(new StorageKey(record.Id.Value, organization.Id));
            Assert.IsNull(await _store.GetAsync<ScratchRecord>(new StorageKey(record.Id.Value, organization.Id)));
        }

        private sealed class TestScratchStorageSettings : IScratchStorageSettings
        {
            public TestScratchStorageSettings(string databaseName)
            {
                DatabaseName = databaseName;
            }

            public System.Collections.Generic.IReadOnlyList<string> Hosts { get; } = new[] { "localhost" };
            public int Port => MongoScratchStoreIntegrationTests.Port;
            public string UserName => MongoScratchStoreIntegrationTests.UserName;
            public string Password => MongoScratchStoreIntegrationTests.Password;
            public string AuthenticationDatabase => "admin";
            public string DatabaseName { get; }
            public string ReplicaSet => null;
            public bool UseTls => false;

            public string BuildConnectionString()
            {
                return $"mongodb://{Uri.EscapeDataString(UserName)}:{Uri.EscapeDataString(Password)}@localhost:{Port}/?authSource=admin";
            }
        }

        private sealed class ScratchRecord : IScratchDataRecord
        {
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public EntityHeader Session { get; set; }
            public string Value { get; set; }
        }
    }
}
