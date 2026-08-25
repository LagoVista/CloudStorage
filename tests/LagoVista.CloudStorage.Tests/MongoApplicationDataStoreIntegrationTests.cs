using LagoVista.CloudStorage.Storage;
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
    public sealed class MongoApplicationDataStoreIntegrationTests
    {
        private IMongoClient _client;
        private ServiceProvider _provider;
        private string _databaseName;

        [OneTimeSetUp]
        public void Setup()
        {
            var connectionString = GetTestConnectionString();
            _databaseName = $"CloudStorageApplicationDataTests_{Guid.NewGuid():N}";
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ApplicationDataStorage:ConnectionString"] = connectionString,
                ["ApplicationDataStorage:DatabaseName"] = _databaseName
            }).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddApplicationDataStorageConnection();
            services.ConfigureApplicationData<ApplicationRecord>(definition =>
            {
                definition.Index(x => x.Category);
                definition.Index(x => x.Reference.Id);
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
        public async Task CrudQueryPagingAndDeterministicCollections_WorkEndToEnd()
        {
            using var scope = _provider.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IApplicationDataStore>();
            var organization = EntityHeader.Create("ORG1", "Organization One");
            var first = CreateRecord(organization, "Bravo");
            var second = CreateRecord(organization, "Alpha");
            var other = new SecondApplicationRecord { Id = NormalizedId32.Factory(), Organization = organization, Value = "other" };

            await store.InsertAsync(first);
            await store.InsertAsync(second);
            await store.InsertAsync(other);

            var originalCreated = first.CreationDate.ToString();
            var originalUpdated = first.LastUpdatedDate.ToString();
            var loaded = await store.GetAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id));
            loaded.Name = "Charlie";
            await Task.Delay(10);
            await store.UpdateAsync(loaded);

            var updated = await store.GetAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id));
            Assert.That(updated.CreationDate.ToString(), Is.EqualTo(originalCreated));
            Assert.That(updated.LastUpdatedDate.ToString(), Is.Not.EqualTo(originalUpdated));

            var page1 = await store.QueryAsync(new StorageQuery<ApplicationRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Reference.Id, StorageFilterOperator.Equal, "REF1")
                .OrderBy(x => x.Name)
                .WithPage(new StoragePageRequest(1)));
            Assert.That(page1.Items.Single().Name, Is.EqualTo("Alpha"));
            Assert.That(page1.HasMoreRecords, Is.True);

            var page2 = await store.QueryAsync(new StorageQuery<ApplicationRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organization.Id)
                .Where(x => x.Reference.Id, StorageFilterOperator.Equal, "REF1")
                .OrderBy(x => x.Name)
                .WithPage(new StoragePageRequest(1, page1.ContinuationToken)));
            Assert.That(page2.Items.Single().Name, Is.EqualTo("Charlie"));

            var database = _client.GetDatabase(_databaseName);
            var names = (await database.ListCollectionNamesAsync()).ToList();
            Assert.That(names, Does.Contain(StorageRecordIdentity.GetCollectionName<ApplicationRecord>()));
            Assert.That(names, Does.Contain(StorageRecordIdentity.GetCollectionName<SecondApplicationRecord>()));

            var raw = database.GetCollection<BsonDocument>(StorageRecordIdentity.GetCollectionName<ApplicationRecord>());
            var indexes = (await raw.Indexes.ListAsync()).ToList().Select(x => x["name"].AsString).ToList();
            Assert.That(indexes, Does.Contain("ix_organization_id"));
            Assert.That(indexes, Does.Contain("ix_category"));
            Assert.That(indexes, Does.Contain("ix_reference_id"));

            await store.DeleteAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id));
            Assert.That(await store.GetAsync<ApplicationRecord>(new StorageKey(first.Id.Value, organization.Id)), Is.Null);
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

        private static string GetTestConnectionString()
        {
            try { return TestConnections.TestMongoDocumentStorage.BuildConnectionString(); }
            catch (Exception ex) { Assert.Ignore($"Local Mongo test harness is not available. {ex.Message}"); return null; }
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
