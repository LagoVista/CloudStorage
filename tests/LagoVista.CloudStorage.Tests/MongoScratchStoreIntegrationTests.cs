using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    [Category("Integration")]
    [Category("Mongo")]
    public sealed class MongoScratchStoreIntegrationTests
    {
        private IMongoClient _client;
        private ServiceProvider _provider;
        private string _databaseName;

        [OneTimeSetUp]
        public void Setup()
        {
            var connectionString = GetTestConnectionString();
            _databaseName = $"CloudStorageScratchTests_{Guid.NewGuid():N}";
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ScratchStorage:ConnectionString"] = connectionString,
                ["ScratchStorage:DatabaseName"] = _databaseName
            }).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScratchStorageConnection();
            services.ConfigureScratchData<ScratchRecord>(definition =>
            {
                definition.Index(x => x.Session.Id);
                definition.RetainFor(TimeSpan.FromMinutes(5));
            });
            _provider = services.BuildServiceProvider();
            _client = new MongoClient(connectionString);
        }

        [OneTimeTearDown]
        public async Task TearDownAsync()
        {
            if (_client != null && !String.IsNullOrWhiteSpace(_databaseName))
                await _client.DropDatabaseAsync(_databaseName);
            _provider?.Dispose();
        }

        [Test]
        public async Task UpsertQueryAndProviderOwnedTtl_WorkEndToEnd()
        {
            using var scope = _provider.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IScratchStore>();
            var organization = EntityHeader.Create("ORG2", "Organization Two");
            var record = new ScratchRecord
            {
                Id = NormalizedId32.Factory(),
                Organization = organization,
                Session = EntityHeader.Create("SESSION1", "Session One"),
                Value = "first"
            };

            await store.UpsertAsync(record);
            record.Value = "second";
            await store.UpsertAsync(record);

            var loaded = await store.GetAsync<ScratchRecord>(new StorageKey(record.Id.Value, organization.Id));
            Assert.That(loaded.Value, Is.EqualTo("second"));

            var page = await store.QueryAsync(new StorageQuery<ScratchRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Session.Id, StorageFilterOperator.Equal, "SESSION1"));
            Assert.That(page.Items.Select(x => x.Id.Value), Does.Contain(record.Id.Value));

            var rawCollection = _client.GetDatabase(_databaseName)
                .GetCollection<BsonDocument>(StorageRecordIdentity.GetCollectionName<ScratchRecord>());
            var raw = await rawCollection.Find(new BsonDocument("_id", record.Id.Value)).FirstOrDefaultAsync();
            Assert.That(raw.Contains("_storageExpiresUtc"), Is.True);
            Assert.That(raw["_storageExpiresUtc"].BsonType, Is.EqualTo(BsonType.DateTime));

            var indexes = (await rawCollection.Indexes.ListAsync()).ToList().Select(x => x["name"].AsString).ToList();
            Assert.That(indexes, Does.Contain("ix_organization_id"));
            Assert.That(indexes, Does.Contain("ix_session_id"));
            Assert.That(indexes, Does.Contain("ix_storage_expires_utc"));

            await store.DeleteAsync<ScratchRecord>(new StorageKey(record.Id.Value, organization.Id));
            Assert.That(await store.GetAsync<ScratchRecord>(new StorageKey(record.Id.Value, organization.Id)), Is.Null);
        }

        private static string GetTestConnectionString()
        {
            try { return TestConnections.TestMongoDocumentStorage.BuildConnectionString(); }
            catch (Exception ex) { Assert.Ignore($"Local Mongo test harness is not available. {ex.Message}"); return null; }
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
