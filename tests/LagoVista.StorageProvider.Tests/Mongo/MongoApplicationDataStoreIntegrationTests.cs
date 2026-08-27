using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core;
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
    [TestCategory("ApplicationData")]
    public sealed class MongoApplicationDataStoreIntegrationTests
    {
        private const string UserName = "nuviot-test";
        private const string Password = "nuviot-test-password";
        private const int Port = 27018;

        private IMongoClient _mongoClient;
        private ServiceProvider _serviceProvider;
        private IApplicationDataStore _store;
        private string _databaseName;

        [TestInitialize]
        public void Setup()
        {
            _databaseName = $"appdata_tests_{Guid.NewGuid():N}";
            var settings = new TestApplicationDataStorageSettings(_databaseName);

            var services = new ServiceCollection();
            services.ConfigureApplicationData<ApplicationRecord>(definition =>
            {
                definition.Index(x => x.Category);
                definition.Index(x => x.Reference.Id);
            });

            _serviceProvider = services.BuildServiceProvider();
            var clientFactory = new MongoStorageClientFactory();
            _store = new MongoApplicationDataStore(settings, clientFactory, _serviceProvider);
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
        public async Task CrudQueryPagingAndDeterministicCollections_WorkEndToEnd()
        {
            var organization = EntityHeader.Create("ORG1", "Organization One");
            var first = CreateRecord(organization, "Bravo");
            var second = CreateRecord(organization, "Alpha");
            var other = new SecondApplicationRecord
            {
                Id = NormalizedId32.Factory(),
                Organization = organization,
                Value = "other"
            };

            await _store.InsertAsync(first);
            await _store.InsertAsync(second);
            await _store.InsertAsync(other);

            Assert.IsFalse(first.CreationDate.IsEmpty);
            Assert.IsFalse(first.LastUpdatedDate.IsEmpty);

            var originalCreated = first.CreationDate.ToString();
            var originalUpdated = first.LastUpdatedDate.ToString();
            var loaded = await _store.GetAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id));
            Assert.IsNotNull(loaded);

            loaded.Name = "Charlie";
            await Task.Delay(10);
            await _store.UpdateAsync(loaded);

            var updated = await _store.GetAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id));
            Assert.IsNotNull(updated);
            Assert.AreEqual(originalCreated, updated.CreationDate.ToString());
            Assert.AreNotEqual(originalUpdated, updated.LastUpdatedDate.ToString());

            var page1 = await _store.QueryAsync(new StorageQuery<ApplicationRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Reference.Id, StorageFilterOperator.Equal, "REF1")
                .OrderBy(x => x.Name)
                .WithPage(new StoragePageRequest(1)));

            Assert.AreEqual(1, page1.Items.Count);
            Assert.AreEqual("Alpha", page1.Items.Single().Name);
            Assert.IsTrue(page1.HasMoreRecords);
            Assert.IsFalse(String.IsNullOrWhiteSpace(page1.ContinuationToken));

            var page2 = await _store.QueryAsync(new StorageQuery<ApplicationRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Reference.Id, StorageFilterOperator.Equal, "REF1")
                .OrderBy(x => x.Name)
                .WithPage(new StoragePageRequest(1, page1.ContinuationToken)));

            Assert.AreEqual(1, page2.Items.Count);
            Assert.AreEqual("Charlie", page2.Items.Single().Name);

            var database = _mongoClient.GetDatabase(_databaseName);
            var collectionNames = (await database.ListCollectionNamesAsync()).ToList();
            CollectionAssert.Contains(collectionNames, StorageRecordIdentity.GetCollectionName<ApplicationRecord>());
            CollectionAssert.Contains(collectionNames, StorageRecordIdentity.GetCollectionName<SecondApplicationRecord>());

            var raw = database.GetCollection<BsonDocument>(StorageRecordIdentity.GetCollectionName<ApplicationRecord>());
            var indexNames = (await raw.Indexes.ListAsync()).ToList().Select(x => x["name"].AsString).ToList();
            CollectionAssert.Contains(indexNames, "ix_organization_id");
            CollectionAssert.Contains(indexNames, "ix_category");
            CollectionAssert.Contains(indexNames, "ix_reference_id");

            await _store.DeleteAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id));
            var deleted = await _store.GetAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id));
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task Insert_PreservesAuthoritativeSourceTimestamps()
        {
            var organization = EntityHeader.Create("ORG1", "Organization One");
            var record = CreateRecord(organization, "Imported");
            var created = UtcTimestamp.FromDateTime(DateTime.UtcNow.AddDays(-2));
            var updated = UtcTimestamp.FromDateTime(DateTime.UtcNow.AddDays(-1));
            record.CreationDate = created;
            record.LastUpdatedDate = updated;

            await _store.InsertAsync(record);

            var loaded = await _store.GetAsync<ApplicationRecord>(new StorageKey(record.Id.Value, organization.Id));
            Assert.IsNotNull(loaded);
            Assert.AreEqual(created.ToString(), loaded.CreationDate.ToString());
            Assert.AreEqual(updated.ToString(), loaded.LastUpdatedDate.ToString());
        }

        private static ApplicationRecord CreateRecord(EntityHeader organization, string name)
        {
            return new ApplicationRecord
            {
                Id = NormalizedId32.Factory(),
                Organization = organization,
                Name = name,
                Category = "active",
                Reference = EntityHeader.Create("REF1", "Reference One")
            };
        }

        private sealed class TestApplicationDataStorageSettings : IApplicationDataStorageSettings
        {
            public TestApplicationDataStorageSettings(string databaseName)
            {
                DatabaseName = databaseName;
            }

            public System.Collections.Generic.IReadOnlyList<string> Hosts { get; } = new[] { "localhost" };
            public int Port => MongoApplicationDataStoreIntegrationTests.Port;
            public string UserName => MongoApplicationDataStoreIntegrationTests.UserName;
            public string Password => MongoApplicationDataStoreIntegrationTests.Password;
            public string AuthenticationDatabase => "admin";
            public string DatabaseName { get; }
            public string ReplicaSet => null;
            public bool UseTls => false;

            public string BuildConnectionString()
            {
                return $"mongodb://{Uri.EscapeDataString(UserName)}:{Uri.EscapeDataString(Password)}@localhost:{Port}/?authSource=admin";
            }
        }

        private sealed class ApplicationRecord : IApplicationDataRecord
        {
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public UtcTimestamp CreationDate { get; set; }
            public UtcTimestamp LastUpdatedDate { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public EntityHeader Reference { get; set; }
        }

        private sealed class SecondApplicationRecord : IApplicationDataRecord
        {
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public UtcTimestamp CreationDate { get; set; }
            public UtcTimestamp LastUpdatedDate { get; set; }
            public string Value { get; set; }
        }
    }
}
