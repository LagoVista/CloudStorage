using LagoVista.AI.Models;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [TestCategory("Integration")]
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
        public async Task ActualVisitorFlowState_NestedPersonaFirst_QueryRemovesVersionMetadata()
        {
            var organization = EntityHeader.Create("ORG1", "Organization One");
            var state = new ConversationFlowState
            {
                Id = NormalizedId32.Factory(),
                Organization = organization,
                Key = "app-trex-jennifer-welcome",
                Name = "Welcome",
                Objective = "Help the visitor begin",
                ConversationStrategy = "Ask one relevant question"
            };

            // The production persona can register this exact class map as a nested value.
            new ConversationFlowDefinition
            {
                InitialNodeKey = "welcome",
                Nodes = new System.Collections.Generic.List<ConversationFlowNode>
                {
                    new ConversationFlowNode
                    {
                        Key = "welcome", StateKey = state.Key, ResolvedState = state
                    }
                }
            }.ToBsonDocument();

            Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(ConversationFlowState)));
            await _store.InsertAsync(state);

            var raw = _mongoClient.GetDatabase(_databaseName)
                .GetCollection<BsonDocument>(StorageRecordIdentity.GetCollectionName<ConversationFlowState>());
            var document = await raw.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            Assert.IsNotNull(document);
            Assert.IsTrue(document.Contains("_storageVersion"));

            var query = new StorageQuery<ConversationFlowState>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Key, StorageFilterOperator.Equal, state.Key)
                .WithPage(new StoragePageRequest(3));

            var page = await _store.QueryAsync(query);
            Assert.AreEqual(1, page.Items.Count);
            Assert.AreEqual(state.Key, page.Items.Single().Key);

            var loaded = await _store.GetAsync<ConversationFlowState>(
                new StorageKey(state.Id.Value, organization.Id));
            Assert.IsNotNull(loaded);
            Assert.AreEqual(state.Key, loaded.Key);
        }

        [TestMethod]
        public async Task NestedStateClassMapFirst_ApplicationDataGetAndQueryIgnoreProviderMetadata()
        {
            var organization = EntityHeader.Create("ORG1", "Organization One");
            var state = new NestedStateRecord
            {
                Id = NormalizedId32.Factory(),
                Organization = organization,
                Key = "welcome"
            };

            // Match a persona document initializing the nested state BSON map first.
            new PersonaStateEnvelope { State = state }.ToBsonDocument();
            Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(NestedStateRecord)));

            await _store.InsertAsync(state);

            var raw = _mongoClient.GetDatabase(_databaseName)
                .GetCollection<BsonDocument>(StorageRecordIdentity.GetCollectionName<NestedStateRecord>());
            var document = await raw.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            Assert.IsTrue(document.Contains("_storageVersion"), "Concurrency metadata must be preserved in Mongo.");

            var loaded = await _store.GetAsync<NestedStateRecord>(new StorageKey(state.Id.Value, organization.Id));
            Assert.IsNotNull(loaded);
            Assert.AreEqual("welcome", loaded.Key);

            var results = await _store.QueryAsync(new StorageQuery<NestedStateRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Key, StorageFilterOperator.Equal, "welcome")
                .WithPage(new StoragePageRequest(3)));
            Assert.AreEqual(1, results.Items.Count);
            Assert.AreEqual(state.Id, results.Items.Single().Id);
        }

        [TestMethod]
        public async Task ConditionalMutation_RejectsStaleWriterAtomically()
        {
            var organization = EntityHeader.Create("ORG1", "Organization One");
            var record = CreateRecord(organization, "Initial");
            await _store.InsertAsync(record);

            var key = new StorageKey(record.Id.Value, organization.Id);
            var writerA = await _store.GetVersionedAsync<ApplicationRecord>(key);
            var writerB = await _store.GetVersionedAsync<ApplicationRecord>(key);

            Assert.IsNotNull(writerA);
            Assert.IsNotNull(writerB);
            Assert.AreEqual(writerA.ConcurrencyToken, writerB.ConcurrencyToken);

            writerA.Record.Name = "Writer A";
            writerB.Record.Name = "Writer B";

            var accepted = await _store.UpdateIfVersionAsync(writerA.Record, writerA.ConcurrencyToken);
            Assert.IsNotNull(accepted);
            Assert.AreNotEqual(writerA.ConcurrencyToken, accepted);

            await Assert.ThrowsExceptionAsync<RecordConflictException>(
                async () => await _store.UpdateIfVersionAsync(writerB.Record, writerB.ConcurrencyToken));

            var persisted = await _store.GetVersionedAsync<ApplicationRecord>(key);
            Assert.IsNotNull(persisted);
            Assert.AreEqual("Writer A", persisted.Record.Name);
            Assert.AreEqual(accepted, persisted.ConcurrencyToken);

            var missing = CreateRecord(organization, "Missing");
            await Assert.ThrowsExceptionAsync<RecordNotFoundException>(
                async () => await _store.UpdateIfVersionAsync(missing, writerA.ConcurrencyToken));

            var otherOrganization = EntityHeader.Create("ORG2", "Organization Two");
            var wrongTenantRecord = CreateRecord(otherOrganization, "Wrong Tenant");
            wrongTenantRecord.Id = record.Id;
            await Assert.ThrowsExceptionAsync<RecordNotFoundException>(
                async () => await _store.UpdateIfVersionAsync(wrongTenantRecord, accepted));
        }

        [TestMethod]
        public async Task ConditionalMutation_ConcurrentWritersAllowOnlyOneAcceptedValue()
        {
            var organization = EntityHeader.Create("ORG1", "Organization One");
            var record = CreateRecord(organization, "Initial");
            await _store.InsertAsync(record);

            var key = new StorageKey(record.Id.Value, organization.Id);
            var writerA = await _store.GetVersionedAsync<ApplicationRecord>(key);
            var writerB = await _store.GetVersionedAsync<ApplicationRecord>(key);
            writerA.Record.Name = "Writer A";
            writerB.Record.Name = "Writer B";

            var updates = await Task.WhenAll(
                TryUpdateAsync(writerA.Record, writerA.ConcurrencyToken),
                TryUpdateAsync(writerB.Record, writerB.ConcurrencyToken));

            Assert.AreEqual(1, updates.Count(succeeded => succeeded));
            Assert.AreEqual(1, updates.Count(succeeded => !succeeded));

            var persisted = await _store.GetVersionedAsync<ApplicationRecord>(key);
            Assert.IsTrue(persisted.Record.Name == "Writer A" || persisted.Record.Name == "Writer B");
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

        private async Task<bool> TryUpdateAsync(ApplicationRecord record, ApplicationDataConcurrencyToken token)
        {
            try
            {
                await _store.UpdateIfVersionAsync(record, token);
                return true;
            }
            catch (RecordConflictException)
            {
                return false;
            }
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

            public bool DirectConnect => true;

            public string BuildConnectionString()
            {
                return $"mongodb://{Uri.EscapeDataString(UserName)}:{Uri.EscapeDataString(Password)}@localhost:{Port}/?authSource=admin";
            }
        }

        private sealed class PersonaStateEnvelope
        {
            public NestedStateRecord State { get; set; }
        }

        private sealed class NestedStateRecord : IApplicationDataRecord
        {
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public UtcTimestamp CreationDate { get; set; }
            public UtcTimestamp LastUpdatedDate { get; set; }
            public string Key { get; set; }
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
